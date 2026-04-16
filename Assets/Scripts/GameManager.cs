using UnityEngine;

/// <summary>
/// ゲーム全体を管理するシングルトン。
/// 各マネージャーの初期化とジェネレーターの配置を行う。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsRunMode { get; private set; } = false;

    [Header("Generator Settings")]
    [SerializeField] private Vector2Int generator1Pos = new Vector2Int(1, 8);
    [SerializeField] private Vector2Int generator2Pos = new Vector2Int(9, 8);

    [Header("Generator 1")]
    [SerializeField] private string gen1Text = "木";
    [SerializeField] private Color gen1Color = new Color(0.9f, 0.4f, 0.3f, 1f);
    [SerializeField] private ConveyorDirection gen1Direction = ConveyorDirection.Right;

    [Header("Generator 2")]
    [SerializeField] private string gen2Text = "木";
    [SerializeField] private Color gen2Color = new Color(0.3f, 0.5f, 0.9f, 1f);
    [SerializeField] private ConveyorDirection gen2Direction = ConveyorDirection.Left;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (GetComponent<KanjiDatabaseManager>() == null)
            gameObject.AddComponent<KanjiDatabaseManager>();
        if (GetComponent<ScoreManager>() == null)
            gameObject.AddComponent<ScoreManager>();
        if (GetComponent<FacilityPlacementManager>() == null)
            gameObject.AddComponent<FacilityPlacementManager>();
        if (GetComponent<BuildModeController>() == null)
            gameObject.AddComponent<BuildModeController>();
    }

    private void Start()
    {
        InitializeGame();
    }

    /// <summary>
    /// ゲームの初期化
    /// </summary>
    private void InitializeGame()
    {
        // 1. グリッドを生成
        GridManager.Instance.GenerateGrid();

        // 2. ジェネレーターを配置
        SetupGenerators();

        // 3. テスト用コンベアの道を設置（初期動作確認用）
        SetupTestConveyors();

        // 4. カメラ位置を調整
        SetupCamera();

        GameHud.EnsureExists();

        SetRunMode(false); // Start in Build Mode (timeScale = 0)
        Debug.Log("Game initialized! Mode = Build Mode");
    }

    public void SetRunMode(bool runMode)
    {
        IsRunMode = runMode;
        Time.timeScale = runMode ? 1f : 0f;
        if (runMode && BuildModeController.Instance != null)
        {
            BuildModeController.Instance.SelectFacility(FacilityType.None);
        }
    }

    /// <summary>
    /// ジェネレーターの初期配置
    /// </summary>
    private void SetupGenerators()
    {
        // ジェネレーター1（左上付近）
        CreateGenerator(generator1Pos, gen1Text, gen1Color, gen1Direction);

        // ジェネレーター2（右下付近）
        CreateGenerator(generator2Pos, gen2Text, gen2Color, gen2Direction);
    }

    /// <summary>
    /// ジェネレーターを作成して配置
    /// </summary>
    private void CreateGenerator(Vector2Int pos, string text, Color color, ConveyorDirection dir)
    {
        GridCell cell = GridManager.Instance.GetCell(pos);
        if (cell == null)
        {
            Debug.LogError("Generator position out of bounds: " + pos);
            return;
        }

        GameObject genObj = new GameObject("Generator_" + pos.x + "_" + pos.y);
        genObj.transform.position = GridManager.Instance.GridToWorld(pos);
        genObj.transform.SetParent(cell.transform);

        ItemGenerator generator = genObj.AddComponent<ItemGenerator>();
        generator.Initialize(pos, text, color, dir);

        Debug.Log("Generator created at " + pos + " with text: " + text);
    }

    /// <summary>
    /// カメラを2D見下ろし型に設定
    /// </summary>
    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;

        // グリッド全体が見えるようにサイズを計算
        float gridW = GridManager.Instance.Width * GridManager.Instance.CellSize;
        float gridH = GridManager.Instance.Height * GridManager.Instance.CellSize;

        cam.orthographicSize = Mathf.Max(gridW, gridH) * 0.65f; // Slightly larger to accommodate offset

        // カメラをレイアウトに合わせて配置（下部にUIが入るため、Y座標を少し上へずらす）
        float centerX = (GridManager.Instance.Width - 1) * GridManager.Instance.CellSize * 0.5f;
        float centerY = (GridManager.Instance.Height - 1) * GridManager.Instance.CellSize * 0.5f;
        cam.transform.position = new Vector3(centerX, centerY + 2.0f, -10f);

        // 背景色を設定
        cam.backgroundColor = new Color(0.18f, 0.18f, 0.2f, 1f);
    }

    /// <summary>
    /// テスト用のコンベアの道を設置。
    /// ジェネレーター1(1,8)から右へ → (5,8)まで
    /// ジェネレーター2(8,1)から左へ → (5,1)まで、そこから上へ → (5,8)で合流
    /// </summary>
    private void SetupTestConveyors()
    {
        PlaceConveyorWithDirection(new Vector2Int(2, 8), ConveyorDirection.Right);
        PlaceConveyorWithDirection(new Vector2Int(3, 8), ConveyorDirection.Right);
        PlaceConveyorWithDirection(new Vector2Int(4, 8), ConveyorDirection.Right);
        PlaceConveyorWithDirection(new Vector2Int(5, 8), ConveyorDirection.Right);

        PlaceConveyorWithDirection(new Vector2Int(8, 8), ConveyorDirection.Left);
        PlaceConveyorWithDirection(new Vector2Int(7, 8), ConveyorDirection.Left);
        PlaceConveyorWithDirection(new Vector2Int(6, 8), ConveyorDirection.Left);

        Debug.Log("Test conveyors placed!");
    }

    /// <summary>
    /// 指定位置にコンベアを指定方向で配置するヘルパー
    /// </summary>
    private void PlaceConveyorWithDirection(Vector2Int pos, ConveyorDirection dir)
    {
        GridCell cell = GridManager.Instance.GetCell(pos);
        if (cell == null || cell.Type == CellType.Generator) return;

        // コンベアを配置
        GameObject conveyorObj = new GameObject("Conveyor_" + pos.x + "_" + pos.y);
        conveyorObj.transform.position = GridManager.Instance.GridToWorld(pos);
        conveyorObj.transform.SetParent(cell.transform);

        ConveyorBelt conveyor = conveyorObj.AddComponent<ConveyorBelt>();
        conveyor.Initialize(cell, dir);

        cell.Conveyor = conveyor;
        cell.SetCellType(CellType.Conveyor);
    }
}
