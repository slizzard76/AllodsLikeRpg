using Godot;
using System.Collections.Generic;

public partial class Main : Node3D
{
    public List<Unit> _selectedUnits = new();
    private bool _dragging = false;
    private Vector2 _dragStart;
    private Camera3D _camera;

    // Смещение камеры относительно центра отряда
    private Vector3 _cameraTargetOffset = new Vector3(0, 12, 12);

    // Для отрисовки рамки
    private ColorRect _selectionBox;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");

        // Создаем простой UI для рисования рамки выделения
        var canvas = new CanvasLayer();
        AddChild(canvas);

        _selectionBox = new ColorRect
        {
            Color = new Color(0.2f, 0.8f, 0.2f, 0.3f), // Полупрозрачный зеленый
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false
        };
        canvas.AddChild(_selectionBox);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseBtn)
        {
            if (mouseBtn.ButtonIndex == MouseButton.Left)
            {
                if (mouseBtn.Pressed)
                {
                    _dragging = true;
                    _dragStart = mouseBtn.Position;
                }
                else if (_dragging)
                {
                    _dragging = false;
                    _selectionBox.Visible = false;
                    HandleSelection(mouseBtn.Position);
                }
            }
            else if (mouseBtn.ButtonIndex == MouseButton.Right && mouseBtn.Pressed)
            {
                HandleMoveOrder(mouseBtn.Position);
            }
        }
        else if (@event is InputEventMouseMotion && _dragging)
        {
            // Отрисовка рамки при перетаскивании
            var mousePos = ((InputEventMouseMotion)@event).Position;
            if (_dragStart.DistanceTo(mousePos) > 5.0f)
            {
                _selectionBox.Visible = true;
                Vector2 pos = new Vector2(Mathf.Min(_dragStart.X, mousePos.X), Mathf.Min(_dragStart.Y, mousePos.Y));
                Vector2 size = new Vector2(Mathf.Abs(mousePos.X - _dragStart.X), Mathf.Abs(mousePos.Y - _dragStart.Y));
                _selectionBox.Position = pos;
                _selectionBox.Size = size;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        // F5 - Сохранить, F9 - Загрузить
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.F5)
            {
                SaveManager.Instance.SaveGame(this);
            }
            else if (keyEvent.Keycode == Key.F6)
            {
                // 1. Очищаем список выделенных
                _selectedUnits.Clear();

                // 2. Снимаем фокус с UI
                GetViewport().GuiReleaseFocus();

                // 3. Прячем UI
                var uiLayer = GetNodeOrNull<CanvasLayer>("UI");
                if (uiLayer != null)
                {
                    foreach (Node child in uiLayer.GetChildren())
                    {
                        if (child is Control control) control.Visible = false;
                    }
                }

                // 4. Загружаем
                SaveManager.Instance.LoadGame(this);
            }
        }
    }

    private void HandleSelection(Vector2 releasePos)
    {
        // Если мышь почти не двигалась — это клик, а не рамка
        if (_dragStart.DistanceTo(releasePos) < 5.0f)
        {
            HandleClick(releasePos);
        }
        else
        {
            HandleBoxSelection(_dragStart, releasePos);
        }
    }

    private void HandleClick(Vector2 mousePos)
    {
        Vector3 from = _camera.ProjectRayOrigin(mousePos);
        Vector3 dir = _camera.ProjectRayNormal(mousePos);

        var query = PhysicsRayQueryParameters3D.Create(from, from + dir * 1000);
        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);

        Unit clickedUnit = null;
        Enemy clickedEnemy = null;
        Npc clickedNpc = null;
        LootItem clickedLoot = null;
        if (result.Count > 0)
        {
            var collider = result["collider"].As<Node>();
            if (collider is Unit unit) clickedUnit = unit;
            else if (collider is Enemy enemy) clickedEnemy = enemy;
            else if (collider is Npc npc) clickedNpc = npc;
            else if (collider is LootItem loot) clickedLoot = loot;
            else if (collider.GetParent() is LootItem parentLoot) clickedLoot = parentLoot;
        }

        // Если кликнули по луту — подбираем его (если есть выделенный юнит рядом)
        if (clickedLoot != null)
        {
            if (_selectedUnits.Count > 0)
            {
                // Просто телепортируем юнита к луту для простоты (в идеале — заставить дойти)
                var unit = _selectedUnits[0];
                if (unit.GlobalPosition.DistanceTo(clickedLoot.GlobalPosition) < 3.0f)
                {
                    clickedLoot.Collect();
                }
                else
                {
                    // Если далеко — идём к нему
                    unit.SetMoveTarget(clickedLoot.GlobalPosition);
                    GD.Print("Слишком далеко, сначала подойди.");
                }
            }
            return;
        }

        // Если кликнули по врагу — приказываем выделенным юнитам атаковать (ЛКМ)
        if (clickedEnemy != null && _selectedUnits.Count > 0)
        {
            foreach (var u in _selectedUnits)
                u.SetTargetEnemy(clickedEnemy);
            return;
        }

        // Если кликнули по NPC — открываем диалог
        if (clickedNpc != null)
        {
            var dialogueUI = GetNode<DialogueUI>("UI/DialogueBox");
            var qm = QuestManager.Instance;
            clickedNpc.Interact(dialogueUI, qm);
            return;
        }

        // Иначе — обычное выделение
        foreach (var u in _selectedUnits) u.SetSelected(false);
        _selectedUnits.Clear();

        if (clickedUnit != null)
        {
            clickedUnit.SetSelected(true);
            _selectedUnits.Add(clickedUnit);
        }
    }

    private void HandleBoxSelection(Vector2 start, Vector2 end)
    {
        Rect2 box = MakeRect(start, end);
        var newly = new List<Unit>();

        foreach (Node child in GetChildren())
        {
            if (child is Unit unit)
            {
                Vector2 screenPos = _camera.UnprojectPosition(unit.GlobalPosition);
                if (screenPos != Vector2.Zero && box.HasPoint(screenPos))
                {
                    newly.Add(unit);
                }
            }
        }

        foreach (var u in _selectedUnits) u.SetSelected(false);
        _selectedUnits = newly;
        foreach (var u in _selectedUnits) u.SetSelected(true);
    }

    private void HandleMoveOrder(Vector2 mousePos)
    {
        if (_selectedUnits.Count == 0) return;

        Vector3 from = _camera.ProjectRayOrigin(mousePos);
        Vector3 dir = _camera.ProjectRayNormal(mousePos);
        var query = PhysicsRayQueryParameters3D.Create(from, from + dir * 1000);
        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);

        Enemy clickedEnemy = null;

        if (result.Count > 0)
        {
            var collider = result["collider"].As<Node>();
            if (collider is Enemy enemy) clickedEnemy = enemy;
            else if (collider.GetParent() is Enemy parentEnemy) clickedEnemy = parentEnemy;
        }

        // Если ПКМ по врагу — атакуем
        if (clickedEnemy != null)
        {
            foreach (var u in _selectedUnits)
                u.SetTargetEnemy(clickedEnemy);
            return;
        }

        // Если ПКМ по земле — идём
        var plane = new Plane(Vector3.Up, 0.0f);
        var hit = plane.IntersectsRay(from, dir);

        if (hit == null) return;

        Vector3 target = hit.Value;

        for (int i = 0; i < _selectedUnits.Count; i++)
        {
            Vector3 offset = new Vector3((i - _selectedUnits.Count / 2.0f) * 1.2f, 0, 0);
            _selectedUnits[i].SetMoveTarget(target + offset);
        }
    }

    private Rect2 MakeRect(Vector2 a, Vector2 b)
    {
        Vector2 pos = new Vector2(Mathf.Min(a.X, b.X), Mathf.Min(a.Y, b.Y));
        Vector2 size = new Vector2(Mathf.Abs(a.X - b.X), Mathf.Abs(a.Y - b.Y));
        return new Rect2(pos, size);
    }

    public override void _Process(double delta)
    {
        // Проверка на Game Over
        bool anyUnitsAlive = false;
        foreach (Node child in GetChildren())
        {
            if (child is Unit) { anyUnitsAlive = true; break; }
        }

        if (!anyUnitsAlive && _selectedUnits.Count > 0 && !SaveManager.IsLoading) 
        {
            GD.Print("GAME OVER");
            GetTree().Paused = true;
        }

        // Камера следует за выделенным отрядом
        if (_selectedUnits.Count > 0)
        {
            Vector3 center = Vector3.Zero;
            int validUnits = 0;
            foreach (var u in _selectedUnits)
            {
                if (IsInstanceValid(u)) { center += u.GlobalPosition; validUnits++; }
            }

            if (validUnits > 0)
            {
                center /= validUnits;
                Vector3 targetPos = center + _cameraTargetOffset;

                // Плавное движение камеры (Lerp)
                _camera.GlobalPosition = _camera.GlobalPosition.Lerp(targetPos, (float)delta * 3.0f);
            }
        }
    }
}