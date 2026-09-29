using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Bertahan.Core;
using Bertahan.Game;
using Bertahan.UI;
using ThreeNet.Avalonia;

namespace Bertahan;

public enum ShellMode
{
    Boot,
    Opening,
    Menu,
    Loading,
    Playing,
}

/// <summary>
/// The game shell: one 3D view whose scene switches between the opening
/// story, the menu village and a level, with the HUD and a stack of UI
/// screens on top. Owns the run (campaign), the flow between levels and input.
/// </summary>
public partial class MainWindow : Window, IShell
{
    private readonly List<Screen> _screens = [];
    private readonly InputMap _input = new();
    private readonly Hud _hud = new();
    private readonly ShotOptions? _shot;
    private readonly Autopilot? _autopilot;
    private readonly ThreeNet.Scene _bootScene;
    private ThreeNetView _view;
    private OpeningStage? _opening;
    private int _bootFrames;
    private int _loadFrames;
    private bool _paused;
    private bool _resultShown;
    private double _fps = 60;
    private int _shotFrames;

    public MainWindow() : this([]) { }

    public MainWindow(string[] args)
    {
        _shot = ShotOptions.Parse(args);
        Settings = GameSettings.Load();
        if (_shot is not null)
        {
            Settings.Quality = _shot.Quality;
        }

        GpuBackend.Select(Settings);
        Log("backend " + GpuBackend.Active);
        InitializeComponent();
        Audio = new AudioManager(Settings);
        UiSound.Audio = Audio;
        HudHost.Content = _hud;
        if (_shot?.Autoplay == true)
        {
            _autopilot = new Autopilot();
        }

        // the view only starts its loop once it has something to draw
        _bootScene = new ThreeNet.Scene();
        _view = NewView();
        SetScene(_bootScene, _bootScene.AddCamera(ThreeNet.Camera.Perspective(1f), Vector3.UnitZ));
        ApplySettings();

        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, (_, e) => _input.KeyUp(e.Key), RoutingStrategies.Tunnel);
        Root.AddHandler(PointerMovedEvent, OnPointer, RoutingStrategies.Tunnel, handledEventsToo: true);
        Root.AddHandler(PointerPressedEvent, OnPointer, RoutingStrategies.Tunnel, handledEventsToo: true);
        Root.AddHandler(PointerReleasedEvent, OnPointer, RoutingStrategies.Tunnel, handledEventsToo: true);
        Root.AddHandler(PointerWheelChangedEvent, (_, e) => _input.Wheel(e.Delta.Y), RoutingStrategies.Tunnel, handledEventsToo: true);
        Deactivated += (_, _) =>
        {
            _input.Reset();
            if (Mode == ShellMode.Playing && !_paused && Session?.State == SessionState.Playing && _shot is null)
            {
                Pause();
            }
        };
        Closing += (_, _) =>
        {
            Settings.Save();
            Session?.Dispose();
            Audio.Dispose();
        };

        Show(new BootScreen(this));
    }

    /// <summary>The 3D view. Replaced whenever the scene changes (see <see cref="SetScene"/>).</summary>
    private ThreeNetView Viewport => _view;

    public GameSettings Settings { get; }

    public AudioManager Audio { get; }

    public MenuStage? Menu { get; private set; }

    public Campaign? Run { get; private set; }

    public GameSession? Session { get; private set; }

    public ShellMode Mode { get; private set; } = ShellMode.Boot;

    private Screen? Top => _screens.Count > 0 ? _screens[^1] : null;

    // ------------------------------------------------------------------ screens

    public void Show(Screen screen)
    {
        _screens.Add(screen);
        Present();
    }

    public void Replace(Screen screen)
    {
        if (_screens.Count > 0)
        {
            _screens.RemoveAt(_screens.Count - 1);
        }

        Show(screen);
    }

    public void Back()
    {
        if (_screens.Count > 0)
        {
            _screens.RemoveAt(_screens.Count - 1);
        }

        if (_screens.Count == 0)
        {
            if (Mode == ShellMode.Playing)
            {
                ResumeGame();
                return;
            }

            _screens.Add(new TitleScreen(this));
        }

        Present();
    }

    private void Present()
    {
        ScreenHost.Children.Clear();
        if (Top is not { } top)
        {
            _hud.Quiet = false;
            return;
        }

        ScreenHost.Children.Add(top);
        if (Menu is not null && top.StageView is { } view)
        {
            Menu.View = view;
            if (view == MenuView.Village || top is NameScreen or DifficultyScreen)
            {
                Menu.Select(-1);
            }
        }

        _hud.Quiet = Mode == ShellMode.Playing;
        Avalonia.Threading.Dispatcher.UIThread.Post(top.OnShown);
    }

    private void ClearScreens()
    {
        _screens.Clear();
        ScreenHost.Children.Clear();
    }

    public void ToTitle()
    {
        ClearScreens();
        Show(new TitleScreen(this));
    }

    // ------------------------------------------------------------------ flow

    private void EnterMenu()
    {
        Menu ??= new MenuStage(Audio);
        Mode = ShellMode.Menu;
        SetScene(Menu.Scene, Menu.Camera.Node);
        Audio.PlayMusic("music_menu");
        Cursor = Cursor.Default;
    }

    public void PlayOpening()
    {
        ClearScreens();
        _opening?.Dispose();
        _opening = new OpeningStage(Audio);
        Mode = ShellMode.Opening;
        SetScene(_opening.Scene, _opening.Camera.Node);
        Show(new OpeningScreen(this, _opening, FinishOpening));
    }

    private void FinishOpening()
    {
        if (Mode != ShellMode.Opening)
        {
            return;
        }

        Settings.SeenOpening = true;
        Settings.Save();
        EnterMenu();
        _opening?.Dispose();
        _opening = null;
        ToTitle();
    }

    public void StartRun(Campaign run)
    {
        Run = run;
        Settings.SavedRun = run;
        Settings.Save();
        if (Top is TitleScreen)
        {
            // "Lanjutkan": show where the run stands first
            Show(new LevelMapScreen(this));
            return;
        }

        PlayLevel();
    }

    public void PlayLevel()
    {
        if (Run is null)
        {
            return;
        }

        _paused = false;
        _resultShown = false;
        Hud(null);
        Session?.Dispose();
        Session = null;
        Mode = ShellMode.Loading;
        _loadFrames = 0;
        ClearScreens();
        Show(new LoadingScreen(this, Run.LevelDef));
        Audio.StopMusic();
    }

    private void BuildSession()
    {
        Campaign run = Run!;
        Session = new GameSession(run.LevelDef, run.CharacterDef, run.DifficultyDef, Audio, Settings);
        Settings.SavedRun = run;
        Settings.Save();
        Mode = ShellMode.Playing;
        SetScene(Session.Scene, Session.Camera.Node);
        ClearScreens();
        Hud(Session);
        _input.Reset();
        Cursor = new Cursor(StandardCursorType.None);
    }

    private void Hud(GameSession? session)
    {
        _hud.Session = session;
        _hud.Run = Run;
        _hud.Quiet = false;
        _hud.Refresh();
    }

    private void Pause()
    {
        _paused = true;
        _input.Reset();
        Cursor = Cursor.Default;
        Audio.Play("sfx_ui_click", 0.7f, 0.8f);
        Show(new PauseScreen(this));
    }

    public void ResumeGame()
    {
        ClearScreens();
        _paused = false;
        _hud.Quiet = false;
        _input.Reset();
        Cursor = new Cursor(StandardCursorType.None);
    }

    public void RestartLevel() => PlayLevel();

    public void QuitToTitle()
    {
        Session?.Dispose();
        Session = null;
        Hud(null);
        _paused = false;
        EnterMenu();
        ToTitle();
    }

    public void Exit() => Close();

    private void LevelFinished(GameSession s)
    {
        _resultShown = true;
        Campaign run = Run!;
        Cursor = Cursor.Default;
        ScoreEntry entry = new()
        {
            Name = run.PlayerName,
            Score = s.Score,
            Character = run.Character,
            Difficulty = run.Difficulty,
            Cleared = s.State == SessionState.Won,
            Date = DateTime.Now,
        };
        int rank = Settings.RecordScore(s.Level.Def.Id, entry);
        if (s.State == SessionState.Won)
        {
            int stars = 1 + (s.Lives == s.Difficulty.Lives ? 1 : 0) + (s.Player.Health / s.Player.MaxHealth > 0.5f ? 1 : 0);
            bool last = run.IsLastLevel;
            run.LevelWon(s.Score, s.Kills);
            Settings.SavedRun = last ? null : run;
            Settings.Save();
            Show(new ResultScreen(this, ResultKind.Won, s, rank, stars));
        }
        else
        {
            LevelOutcome outcome = run.LevelLost();
            Settings.SavedRun = run;
            Settings.Save();
            Show(new ResultScreen(this, outcome == LevelOutcome.Retry ? ResultKind.Retry : ResultKind.GameOver, s, rank, 0));
        }
    }

    // ------------------------------------------------------------------ settings

    public void ApplySettings()
    {
        Viewport.RendererOptions = Graphics.Options(Settings.Quality);
        Viewport.RenderScale = Graphics.Scale(Settings.Quality);
        _hud.ShowFps = Settings.ShowFps;
        if (_shot is null)
        {
            WindowState = Settings.Fullscreen ? WindowState.FullScreen : WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState;
        }
    }

    /// <summary>
    /// Shows another scene. Three.Net's renderer caches GPU resources by handle and
    /// mixes up meshes and materials when it later draws a different scene, so every
    /// scene change gets a fresh view (and with it a fresh renderer).
    /// </summary>
    private void SetScene(ThreeNet.Scene scene, ThreeNet.Node camera)
    {
        if (_view.Scene is { } current && current != scene && current != _bootScene)
        {
            _view = NewView();
            ApplySettings();
        }

        _view.Scene = scene;
        _view.Camera = camera;
    }

    private ThreeNetView NewView()
    {
        ThreeNetView view = new() { Focusable = false, MaxFramesPerSecond = 120 };
        view.Frame += OnFrame;
        view.RendererCreated += (_, _) => Log("renderer created " + view.Renderer?.AdapterName);
        view.RenderFailed += (_, msg) =>
        {
            Console.Error.WriteLine("Render failed: " + msg);
            if (_shot is not null)
            {
                File.WriteAllText(_shot.Path + ".txt", "RENDER FAILED: " + msg);
                Close();
            }
        };
        if (_view is { } old)
        {
            old.Frame -= OnFrame;
            old.IsRendering = false;
            Root.Children.Remove(old);
        }

        Root.Children.Insert(0, view);
        return view;
    }

    // ------------------------------------------------------------------ input

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            Settings.Fullscreen = !Settings.Fullscreen;
            ApplySettings();
            Settings.Save();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F12)
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Bertahan");
            Directory.CreateDirectory(dir);
            Capture(Path.Combine(dir, $"bertahan_{DateTime.Now:yyyyMMdd_HHmmss}.png"));
            e.Handled = true;
            return;
        }

        bool playing = Mode == ShellMode.Playing && !_paused && Top is null;
        if (playing)
        {
            if (e.Key is Key.Escape or Key.P)
            {
                if (Session?.State == SessionState.Playing)
                {
                    Pause();
                }

                e.Handled = true;
                return;
            }

            _input.KeyDown(e.Key);
            e.Handled = true;
            return;
        }

        // screens: let focused controls act first, then the screen's own keys
        if (Top is { } top && e.Source is not TextBox)
        {
            if (e.Key is Key.Escape or Key.Up or Key.Down or Key.Left or Key.Right or Key.W or Key.S || top is OpeningScreen)
            {
                top.HandleKey(e);
            }
        }
        else if (Top is { } t && e.Key == Key.Escape)
        {
            t.HandleKey(e);
        }
    }

    private void OnPointer(object? sender, PointerEventArgs e)
    {
        Point p = e.GetPosition(Viewport);
        _input.Mouse = new Vector2((float)p.X, (float)p.Y);
        _input.MouseValid = true;
        PointerPointProperties props = e.GetCurrentPoint(Viewport).Properties;
        if (Mode == ShellMode.Playing && !_paused && Top is null)
        {
            _input.Pointer(props.IsLeftButtonPressed, props.IsRightButtonPressed);
        }
    }

    // ------------------------------------------------------------------ frame

    private static readonly string? LogPath = Environment.GetEnvironmentVariable("BERTAHAN_LOG");

    public static void Log(string message)
    {
        if (LogPath is not null)
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
    }

    private void OnFrame(object? sender, FrameEventArgs e)
    {

        float dt = Math.Clamp(e.DeltaSeconds, 0.001f, 0.05f);
        _fps = (_fps * 0.95) + (0.05 / Math.Max(e.DeltaSeconds, 0.001));
        Vector2 size = new((float)Math.Max(1, Viewport.Bounds.Width), (float)Math.Max(1, Viewport.Bounds.Height));

        switch (Mode)
        {
            case ShellMode.Boot:
                // let the boot screen show for a couple of frames before the heavy loading
                if (++_bootFrames == 3)
                {
                    Boot();
                }

                break;
            case ShellMode.Opening when _opening is not null:
                _opening.Camera.ViewportSize = size;
                _opening.Update(dt);
                if (_opening.Finished && _shot is null)
                {
                    FinishOpening();
                }

                break;
            case ShellMode.Menu when Menu is not null:
                Menu.Camera.ViewportSize = size;
                Menu.PanelOffset = Top is TitleScreen or NameScreen or CharacterScreen ? 1f : 0f;
                Menu.Update(dt);
                break;
            case ShellMode.Loading:
                if (++_loadFrames == 3)
                {
                    BuildSession();
                }

                break;
            case ShellMode.Playing when Session is not null:
                Session.Camera.ViewportSize = size;
                if (!_paused)
                {
                    InputState input = _autopilot is not null ? _autopilot.Next(Session, dt) : _input.Build(Settings.CameraSpeed);
                    Session.Update(dt, input);
                    if (Session.Finished && !_resultShown)
                    {
                        LevelFinished(Session);
                    }
                }

                break;
        }

        Top?.Tick(dt);
        _hud.Mouse = _input.Mouse;
        _hud.MouseValid = _input.MouseValid && _autopilot is null;
        _hud.Fps = _fps;
        _hud.Refresh();
        if (_shot is not null)
        {
            ShotFrame();
        }
    }

    private void Boot()
    {
        if (_shot is not null)
        {
            _shot.Setup(this);
            return;
        }

        if (!Settings.SeenOpening)
        {
            PlayOpening();
            return;
        }

        EnterMenu();
        ToTitle();
    }

    // ------------------------------------------------------------------ screenshots

    private void Capture(string path)
    {
        try
        {
            if (Viewport.Renderer is { } renderer)
            {
                // the UI layer is rendered without the 3D view and laid over the raw frame
                var background = Root.Background;
                double opacity = Viewport.Opacity;
                Root.Background = null;
                Viewport.Opacity = 0;
                try
                {
                    Screenshot.Save(renderer, path, Root);
                }
                finally
                {
                    Root.Background = background;
                    Viewport.Opacity = opacity;
                }
            }

            Audio.Play("sfx_pickup", 0.6f, 1.4f);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Screenshot failed: " + ex.Message);
        }
    }

    private void ShotFrame()
    {
        if (Mode is ShellMode.Boot or ShellMode.Loading)
        {
            return;
        }

        _shotFrames++;
        if (_shotFrames == 2)
        {
            _shot!.AfterSetup(this);
        }

        if (_shotFrames == _shot!.Frames)
        {
            Capture(_shot.Path);
            string state = Session is { } gs ? $" wave={gs.Waves.Wave + 1}/{gs.Waves.WaveCount} phase={gs.Waves.Phase} score={gs.Score} kills={gs.Kills} hp={gs.Player.Health:0} lives={gs.Lives} state={gs.State} swayers={gs.Atmosphere.Swayers.Count}" : "";
            File.WriteAllText(_shot.Path + ".txt", state + $" mode={Mode} scene={_shot.Scene} fps={_fps:0} draws={Viewport.Stats.DrawCalls} tris={Viewport.Stats.Triangles} backend={GpuBackend.Active}");
            Close();
        }
    }

    /// <summary>Headless runs for the docs: <c>Bertahan.exe --shot out.png --scene title [--level 2] [--warp 30] [--autoplay] [--time 12] [--frames 30]</c>.</summary>
    private sealed class ShotOptions
    {
        public string Path = "shot.png";
        public string Scene = "title";
        public int Frames = 30;
        public int Level = 1;
        public float Warp;
        public float Time;
        public bool Autoplay;
        public int Quality = 2;
        public string Character = "bapak";
        public string Overlay = "";
        public string? Name;
        public int Retries = Campaign.MaxRetries;
        public Difficulty Difficulty = Difficulty.Pemberani;

        public static ShotOptions? Parse(string[] args)
        {
            int i = Array.IndexOf(args, "--shot");
            if (i < 0 || i + 1 >= args.Length)
            {
                return null;
            }

            ShotOptions o = new() { Path = System.IO.Path.GetFullPath(args[i + 1]) };
            for (int k = 0; k < args.Length - 1; k++)
            {
                string v = args[k + 1];
                switch (args[k])
                {
                    case "--scene": o.Scene = v; break;
                    case "--frames": o.Frames = int.Parse(v); break;
                    case "--level": o.Level = int.Parse(v); break;
                    case "--warp": o.Warp = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--time": o.Time = float.Parse(v, System.Globalization.CultureInfo.InvariantCulture); break;
                    case "--quality": o.Quality = int.Parse(v); break;
                    case "--char": o.Character = v; break;
                    case "--overlay": o.Overlay = v; break;
                    case "--name": o.Name = v; break;
                    case "--retries": o.Retries = int.Parse(v); break;
                    case "--difficulty": o.Difficulty = Enum.Parse<Difficulty>(v, true); break;
                }
            }

            o.Autoplay = args.Contains("--autoplay");
            return o;
        }

        private Campaign MakeRun(MainWindow w) => new()
        {
            PlayerName = Name ?? w.Settings.LastName,
            Character = Character,
            Difficulty = Difficulty,
            Level = Level,
            TotalScore = Level > 1 ? 2150 * (Level - 1) : 0,
            RetriesLeft = Retries,
        };

        public void Setup(MainWindow w)
        {
            switch (Scene)
            {
                case "opening":
                    w.PlayOpening();
                    for (float t = 0; t < Time; t += 1f / 30f)
                    {
                        w._opening!.Update(1f / 30f);
                        w.Top?.Tick(1f / 30f);
                    }

                    return;
                case "level":
                    w.EnterMenu();
                    w.Run = MakeRun(w);
                    w.PlayLevel();
                    return;
            }

            w.EnterMenu();
            for (float t = 0; t < Warp; t += 1f / 30f)
            {
                w.Menu!.Update(1f / 30f);
            }

            w.ToTitle();
            Campaign run = MakeRun(w);
            switch (Scene)
            {
                case "name": w.Show(new NameScreen(w)); break;
                case "difficulty": w.Show(new DifficultyScreen(w, run)); break;
                case "chars": w.Show(new CharacterScreen(w, run)); break;
                case "map":
                    w.Run = run;
                    w.Show(new LevelMapScreen(w));
                    break;
                case "scores": w.Show(new TopScoreScreen(w)); break;
                case "options": w.Show(new OptionsScreen(w)); break;
                case "controls": w.Show(new ControlsScreen(w)); break;
                case "about":
                    AboutScreen about = new(w);
                    w.Show(about);
                    for (float t = 0; t < Time; t += 1f / 30f)
                    {
                        about.Tick(1f / 30f);
                    }

                    break;
                case "ending":
                    run.TotalScore = 17650;
                    run.TotalKills = 176;
                    w.Run = run;
                    w.Show(new EndingScreen(w));
                    break;
                case "loading":
                    w.Run = run;
                    w.ClearScreens();
                    w.Show(new LoadingScreen(w, run.LevelDef));
                    break;
            }
        }

        /// <summary>Runs once the level exists: fast-forward the fight, then open an overlay.</summary>
        public void AfterSetup(MainWindow w)
        {
            if (Scene != "level" || w.Session is not { } s)
            {
                return;
            }

            for (float t = 0; t < Warp && !s.Finished; t += 1f / 30f)
            {
                InputState input = w._autopilot?.Next(s, 1f / 30f) ?? new InputState { WeaponSlot = -1 };
                s.Update(1f / 30f, input);
            }

            switch (Overlay)
            {
                case "pause": w.Pause(); break;
                case "result" when s.Finished: w.LevelFinished(s); break;
                case "preview":
                    // a clean cinematic view for loading screens and docs: no HUD, low camera
                    w._paused = true;
                    w.Hud(null);
                    w.Cursor = Cursor.Default;
                    Vector3 p = s.Player.World;
                    Vector3 back = new(MathF.Sin(s.Camera.Yaw), 0, MathF.Cos(s.Camera.Yaw));
                    s.Camera.Place(p + (back * 9f) + new Vector3(2.5f, 4.5f, 0), p + new Vector3(0, 1.2f, 0) - (back * 4f));
                    break;
            }
        }
    }
}

/// <summary>Shown while the menu village loads at start up.</summary>
public sealed class BootScreen : Screen
{
    public BootScreen(IShell shell) : base(shell)
    {
        Background = Avalonia.Media.Brushes.Black;
        Children.Add(new Image { Source = UI.Assets.Image("loading"), Stretch = Avalonia.Media.Stretch.UniformToFill, Opacity = 0.9 });
        Avalonia.Controls.Image logo = UI.Kit.Picture("logo", 640);
        logo.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        Children.Add(logo);
        TextBlock t = UI.Kit.Text("Memuat Kampung Damai...", 22, UI.Kit.SunBrush, Avalonia.Media.FontWeight.Black);
        t.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        t.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
        t.Margin = new Thickness(0, 0, 0, 60);
        Children.Add(t);
        Opacity = 1;
    }

    public override MenuView? StageView => null;

    public override void OnBack()
    {
    }
}
