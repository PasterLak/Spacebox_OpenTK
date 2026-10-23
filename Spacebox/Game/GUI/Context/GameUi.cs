using Spacebox.Game.Player;
using Spacebox.Game.Ships.Sharing;
using Spacebox.Game.Tutorial.UI;
using Spacebox.Game.Waypoints;

namespace Spacebox.Game.GUI;

public sealed class GameUi
{
    private readonly List<IGameUiController> _controllers = new List<IGameUiController>();

    public static GameUi Current { get; private set; }

    public GameUi()
    {
        HitMarker = Add(new HitMarkerUI());
        RepairTarget = Add(new RepairTargetUI());
        ItemControls = Add(new ItemControlsUI());
        GravityIndicator = Add(new GravityIndicatorUI());
        Loading = Add(new WorldLoadingController());
        DnaReconstruction = Add(new DnaReconstructionScreen());
        Panel = new PanelController(ItemControls);
        Chat = Add(new ChatController(Panel));
        Add(Panel);
        Tutorial = Add(new TutorialUI(Loading, Chat));
        TutorialPrompt = Add(new TutorialPromptUI(Tutorial, Loading));
        TutorialMenu = Add(new TutorialMenuUI(Tutorial));
        Waypoints = Add(new WaypointWindowUI(Chat));
        ShipLibrary = Add(new ShipLibraryUI(Chat));
        Processing = Add(new ResourceProcessingGUI());
        Crafting = Add(new CraftingGUI());
        Inventory = Add(new InventoryUI(Chat, ItemControls));
        Storage = Add(new StorageUI());
        Creative = Add(new CreativeWindowUI());
        Equipment = Add(new EquipmentUI());
        Welcome = Add(new WelcomeUI());
        Statistics = Add(new StatisticsUI());
        Pause = Add(new PauseUI(Statistics, TutorialMenu));
        Generator = Add(new GeneratorUI());
        Analyzer = Add(new AnalyzerUI());
        OxygenGenerator = Add(new OxygenGeneratorUI());
        ClaimBeacon = Add(new ClaimBeaconUI());
        Disassembler = Add(new DisassemblerUI());
        GravityGenerator = Add(new GravityGeneratorUI());
        Sign = Add(new SignUI());
        ShipConsole = Add(new ShipConsoleUI(Chat));
        DnaReconstructor = Add(new DnaReconstructorUI());
    }

    public HitMarkerUI HitMarker { get; }
    public RepairTargetUI RepairTarget { get; }
    public ItemControlsUI ItemControls { get; }
    public GravityIndicatorUI GravityIndicator { get; }
    public WorldLoadingController Loading { get; }
    public DnaReconstructionScreen DnaReconstruction { get; }
    public PanelController Panel { get; }
    public ChatController Chat { get; }
    public TutorialUI Tutorial { get; }
    public TutorialPromptUI TutorialPrompt { get; }
    public TutorialMenuUI TutorialMenu { get; }
    public WaypointWindowUI Waypoints { get; }
    public ShipLibraryUI ShipLibrary { get; }
    public ResourceProcessingGUI Processing { get; }
    public CraftingGUI Crafting { get; }
    public InventoryUI Inventory { get; }
    public StorageUI Storage { get; }
    public CreativeWindowUI Creative { get; }
    public EquipmentUI Equipment { get; }
    public WelcomeUI Welcome { get; }
    public StatisticsUI Statistics { get; }
    public PauseUI Pause { get; }
    public GeneratorUI Generator { get; }
    public AnalyzerUI Analyzer { get; }
    public OxygenGeneratorUI OxygenGenerator { get; }
    public ClaimBeaconUI ClaimBeacon { get; }
    public DisassemblerUI Disassembler { get; }
    public GravityGeneratorUI GravityGenerator { get; }
    public SignUI Sign { get; }
    public ShipConsoleUI ShipConsole { get; }
    public DnaReconstructorUI DnaReconstructor { get; }
    public RadarUI Radar { get; private set; }

    public IReadOnlyList<IGameUiController> Controllers => _controllers;

    public void MakeCurrent()
    {
        Current = this;
    }

    public void AttachRadar(RadarUI radar)
    {
        if (Radar != null) _controllers.Remove(Radar);
        Radar = radar;
        if (radar != null) _controllers.Insert(0, radar);
    }

    public void Initialize(LocalAstronaut player)
    {
        for (int i = 0; i < _controllers.Count; i++) _controllers[i].Initialize(player);
    }

    public void Update()
    {
        for (int i = 0; i < _controllers.Count; i++) _controllers[i].Update();
    }

    public void OnGUI()
    {
        for (int i = 0; i < _controllers.Count; i++) _controllers[i].OnGUI();
    }

    public void Reset()
    {
        for (int i = 0; i < _controllers.Count; i++) _controllers[i].Reset();
        if (Current == this) Current = null;
    }

    private T Add<T>(T controller) where T : IGameUiController
    {
        _controllers.Add(controller);
        return controller;
    }
}
