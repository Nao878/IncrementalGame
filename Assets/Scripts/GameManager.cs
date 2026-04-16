using UnityEngine;

/// <summary>
/// ゲーム全体を管理するシングルトン。
/// 各マネージャーの初期化とジェネレーターの配置を行う。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsRunMode { get; private set; } = false;

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

        // 2. メインハブの設置 (2x2)
        SetupMainHub();

        // 3. カメラ位置を調整
        SetupCamera();

        GameHud.EnsureExists();

        SetRunMode(false); // Start in Build Mode (timeScale = 0)
        Debug.Log("Game initialized! Mode = Build Mode");
    }

    private void SetupMainHub()
    {
        // グリッドの中央付近の2x2を使用
        int cx = GridManager.Instance.Width / 2 - 1;
        int cy = GridManager.Instance.Height / 2 - 1;

        GameObject hubObj = new GameObject("MainHub");
        MainHubNode hub = hubObj.AddComponent<MainHubNode>();

        System.Collections.Generic.List<GridCell> cells = new System.Collections.Generic.List<GridCell>();
        for (int y = cy; y < cy + 2; y++)
        {
            for (int x = cx; x < cx + 2; x++)
            {
                var cell = GridManager.Instance.GetCell(new Vector2Int(x, y));
                if (cell != null) cells.Add(cell);
            }
        }
        
        hub.SetupHub(cells);
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
}
