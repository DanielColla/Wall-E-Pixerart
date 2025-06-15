using Godot;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public partial class MainUI : Control
{
    [Export] public TextEdit CodeEditor { get; set; }
    [Export] public CanvasRenderer Canvas { get; set; }
    [Export] public LineEdit CanvasSizeInput { get; set; }
    [Export] public Button RunButton { get; set; }
    [Export] public Button SaveButton { get; set; }
    [Export] public Button LoadButton { get; set; }
    [Export] public Button ClearButton { get; set; }
    [Export] public Button ResizeButton { get; set; }
    [Export] public FileDialog FileDialog { get; set; }
    [Export] public Label StatusLabel { get; set; }
    [Export] public CheckBox GridToggle { get; set; }
    [Export] public TextEdit LineNumbers { get; set; }

    private Interpreter interpreter = new();
    private string currentFilePath = string.Empty;
    private Stack<string> history = new();
    private bool isUndoing = false;

    private const int MinCanvasSize = 16;
    private const int MaxCanvasSize = 1024;

    public override void _Ready()
    {
        try
        {
            ValidateEssentialComponents();
            ConnectSignals();
            InitializeSystem();
        }
        catch (Exception e)
        {
            ShowFatalError("Error inicializando sistema", e);
        }
    }

    private void ValidateEssentialComponents()
    {
        var missing = new List<string>();
        if (CodeEditor == null) missing.Add(nameof(CodeEditor));
        if (Canvas == null) missing.Add(nameof(Canvas));
        if (StatusLabel == null) missing.Add(nameof(StatusLabel));
        if (missing.Count > 0)
            throw new Exception($"Componentes faltantes: {string.Join(", ", missing)}");
    }

    private void ConnectSignals()
    {
        RunButton.Pressed += OnRunPressed;
        SaveButton.Pressed += OnSavePressed;
        LoadButton.Pressed += OnLoadPressed;
        ClearButton.Pressed += OnClearPressed;
        ResizeButton.Pressed += OnResizePressed;
        FileDialog.FileSelected += OnFileSelected;
        GridToggle.Toggled += OnGridToggled;
        CodeEditor.TextChanged += UpdateLineNumbers;
        CodeEditor.GuiInput += OnCodeEditorInput;
    }

    private void InitializeSystem()
    {
        try
        {
            Canvas.Initialize(256);
            interpreter.Canvas = Canvas;
            SetupDefaultTemplate();
            UpdateStatus("Sistema inicializado");
        }
        catch (Exception e)
        {
            throw new WallEException("Error inicializando", 
                WallEException.ErrorType.Ejecucion, inner: e);
        }
    }

    private void SetupDefaultTemplate()
    {
        CodeEditor.Text = @"Spawn(0, 0)
Color(""Black"")
n <- 5
DrawLine(1, 0, n)
";
        SaveState();
    }

    private void OnRunPressed()
    {
        try
        {
            ExecuteProgram();
        }
        catch (WallEException e)
        {
            HandleWallEError(e);
        }
        catch (Exception e)
        {
            ShowRuntimeError("Error inesperado", e);
        }
    }

    private void ExecuteProgram()
    {
        try
        {
            SaveState();
            interpreter = new Interpreter { Canvas = Canvas };
            
            var tokens = new Lexer(CodeEditor.Text).Tokenize();
            var program = new Parser(tokens).ParseProgram();
            
            ValidateProgramStructure(program);
            interpreter.Execute(program);
            
            UpdateStatus("Ejecución exitosa!", false);
        }
        catch (WallEException e)
        {
            HandleWallEError(e);
        }
        catch (Exception e)
        {
            throw new WallEException("Error ejecución", 
                WallEException.ErrorType.Ejecucion, inner: e);
        }
    }

    private void ValidateProgramStructure(ProgramNode program)
    {
        if (program.Statements.Count == 0 || !(program.Statements[0] is CommandNode cmd) || cmd.Command != "Spawn")
        {
            throw new WallEException("Debe comenzar con Spawn()", 
                WallEException.ErrorType.Semantico, 1);
        }
    }

    private void HighlightErrorLine(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > CodeEditor.GetLineCount()) return;
        
        CodeEditor.SetCaretLine(lineNumber - 1);
        CodeEditor.Call("scroll_to_line", lineNumber - 1);
        CodeEditor.SetLineBackgroundColor(lineNumber - 1, new Color(1f, 0.2f, 0.2f, 0.4f));
    }

   private void HandleWallEError(WallEException e)
{
    string errorType = e.Type switch
    {
        WallEException.ErrorType.Sintaxis => "Sintáctico",
        WallEException.ErrorType.Semantico => "Semántico",
        _ => "Ejecución"
    };
    
    string detailedMessage = e.Message;
    
    if (e.InnerException != null)
    {
        detailedMessage += $"\nDetalles: {e.InnerException.Message}";
    }
    
    if (!string.IsNullOrEmpty(e.Context))
    {
        detailedMessage += $"\nContexto: {e.Context}";
    }
    
    UpdateStatus($"ERROR ({errorType}, Línea {e.Line}): {detailedMessage}", true);
    HighlightErrorLine(e.Line);
    GD.PrintErr($"{detailedMessage}\nStack Trace: {e.StackTrace}");
}
    private void OnCodeEditorInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Z && keyEvent.CtrlPressed)
            {
                UndoLastAction();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void SaveState()
    {
        if (!isUndoing)
        {
            history.Push(CodeEditor.Text);
        }
    }

    private void UndoLastAction()
    {
        if (history.Count > 0)
        {
            isUndoing = true;
            CodeEditor.Text = history.Pop();
            isUndoing = false;
            UpdateStatus("Acción deshecha");
        }
    }

    private void UpdateStatus(string message, bool isError = false)
    {
        StatusLabel.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
        StatusLabel.Modulate = isError ? new Color(1, 0.2f, 0.2f) : Colors.White;
        StatusLabel.TooltipText = message;
    }

    private void OnGridToggled(bool toggled)
    {
        Canvas.ToggleGrid(toggled);
        UpdateStatus($"Cuadrícula {(toggled ? "activada" : "desactivada")}");
    }

    private void UpdateLineNumbers()
    {
        try
        {
            if (LineNumbers == null || CodeEditor == null) return;
            
            var lines = CodeEditor.Text.Split('\n');
            LineNumbers.Text = string.Join("\n", Enumerable.Range(1, lines.Length).Select(n => $"{n}."));
            LineNumbers.ScrollVertical = CodeEditor.ScrollVertical;
            LineNumbers.ScrollHorizontal = CodeEditor.ScrollHorizontal;
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error actualizando números de línea: {e}");
        }
    }

    private void OnSavePressed()
    {
        FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
        FileDialog.Title = "Guardar programa";
        FileDialog.Access = FileDialog.AccessEnum.Filesystem;
        FileDialog.Filters = new[] { "*.pw ; Archivos Wall-E" };
        FileDialog.PopupCentered(new Vector2I(600, 400));
    }

    private void OnLoadPressed()
    {
        FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        FileDialog.Title = "Cargar programa";
        FileDialog.Access = FileDialog.AccessEnum.Filesystem;
        FileDialog.Filters = new[] { "*.pw ; Archivos Wall-E" };
        FileDialog.PopupCentered(new Vector2I(600, 400));
    }

    private void OnClearPressed()
    {
        try
        {
            CodeEditor.Text = "";
            Canvas.ClearCanvas();
            interpreter = new Interpreter { Canvas = Canvas };
            UpdateStatus("Editor y canvas reiniciados");
            history.Clear();
        }
        catch (Exception e)
        {
            ShowRuntimeError("Error al limpiar el sistema", e);
        }
    }

    private void OnResizePressed()
    {
        try
        {
            if (!int.TryParse(CanvasSizeInput.Text, out int newSize))
                throw new WallEException("Formato de tamaño inválido", WallEException.ErrorType.Semantico);
            
            if (newSize < MinCanvasSize || newSize > MaxCanvasSize)
                throw new WallEException($"Tamaño debe estar entre {MinCanvasSize} y {MaxCanvasSize}", WallEException.ErrorType.Semantico);
            
            Canvas.Initialize(newSize);
            interpreter = new Interpreter { Canvas = Canvas };
            UpdateStatus($"Canvas redimensionado a {newSize}x{newSize}");
        }
        catch (WallEException e)
        {
            HandleWallEError(e);
        }
        catch (Exception e)
        {
            ShowRuntimeError("Error al redimensionar el canvas", e);
        }
    }

    private void OnFileSelected(string path)
    {
        try
        {
            if (FileDialog.FileMode == FileDialog.FileModeEnum.SaveFile)
            {
                SaveProgram(path);
            }
            else
            {
                LoadProgram(path);
            }
        }
        catch (WallEException e)
        {
            HandleWallEError(e);
        }
        catch (Exception e)
        {
            ShowRuntimeError("Error de archivo", e);
        }
    }

    private void SaveProgram(string path)
    {
        try
        {
            File.WriteAllText(path, CodeEditor.Text);
            currentFilePath = path;
            UpdateStatus($"Programa guardado: {Path.GetFileName(path)}");
            SaveState();
        }
        catch (UnauthorizedAccessException)
        {
            throw new WallEException("Permisos insuficientes para guardar", WallEException.ErrorType.Ejecucion);
        }
        catch (Exception ex)
        {
            throw new WallEException($"Error al guardar: {ex.Message}", WallEException.ErrorType.Ejecucion);
        }
    }

    private void LoadProgram(string path)
    {
        try
        {
            if (!File.Exists(path))
                throw new WallEException("Archivo no encontrado", WallEException.ErrorType.Ejecucion);
            
            CodeEditor.Text = File.ReadAllText(path);
            currentFilePath = path;
            UpdateStatus($"Programa cargado: {Path.GetFileName(path)}");
            SaveState();
        }
        catch (IOException)
        {
            throw new WallEException("Error de lectura del archivo", WallEException.ErrorType.Ejecucion);
        }
        catch (Exception ex)
        {
            throw new WallEException($"Error al cargar: {ex.Message}", WallEException.ErrorType.Ejecucion);
        }
    }

    private void ShowFatalError(string message, Exception e)
    {
        GD.PrintErr($"[FATAL] {message}: {e}");
        UpdateStatus($"{message}. Ver consola para detalles.", isError: true);
    }

    private void ShowRuntimeError(string context, Exception e)
    {
        UpdateStatus($"{context}: {e.Message}", isError: true);
        GD.PrintErr($"{context}: {e}\nStack Trace: {e.StackTrace}");
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMSizeChanged)
        {
            UpdateLineNumbers();
        }
    }
}