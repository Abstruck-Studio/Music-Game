using Godot;
using System;
using System.Collections.Generic;

public partial class InfiniteSpawner : Node3D
{
	[Export] public PackedScene ModelScene; // 要生成的模型场景
    [Export] public float MovementSpeed = 5.0f; // 移动速度
    [Export] public float SpawnInterval = 1.0f; // 生成间隔(秒)
    [Export] public float MaxDistance = 100.0f; // 最大移动距离
    
    // 对象池
    private const int POOL_SIZE = 20;
    private Queue<Node3D> _objectPool = new Queue<Node3D>();
    private float _spawnTimer = 0.0f;
    
    // 固定生成位置
    private readonly Vector3 _spawnPosition = new Vector3(-2.5f, 0f, -25f);

    public override void _Ready()
    {
        // 创建对象池
        for (int i = 0; i < POOL_SIZE; i++)
        {
            Node3D instance = ModelScene.Instantiate<Node3D>();
            AddChild(instance);
            instance.Visible = false;
            _objectPool.Enqueue(instance);
        }
    }

    public override void _Process(double delta)
    {
        // 更新计时器
        _spawnTimer += (float)delta;
        
        if (_spawnTimer >= SpawnInterval)
        {
            SpawnObject();
            _spawnTimer = 0.0f;
        }
        
        // 更新所有活动对象
        UpdateObjects((float)delta);
    }

    private void SpawnObject()
    {
        if (_objectPool.Count == 0) return;
        
        // 从池中取出对象
        Node3D newObject = _objectPool.Dequeue();
        newObject.Visible = true;
        
        // 设置固定位置
        newObject.Position = _spawnPosition;
        
        // 重置移动状态
        newObject.SetMeta("IsMoving", true);
    }

    private void UpdateObjects(float delta)
    {
        foreach (Node3D obj in GetChildren())
        {
            if (!obj.Visible) continue;
            
            // 检查是否在移动
            if ((bool)obj.GetMeta("IsMoving"))
            {
                // 沿Z轴正方向移动
                obj.Translate(-Vector3.Forward * MovementSpeed * delta);
                
                // 检查是否超出范围
                if (obj.Position.Z > _spawnPosition.Z + MaxDistance)
                {
                    RecycleObject(obj);
                }
            }
        }
    }

    private void RecycleObject(Node3D obj)
    {
        obj.Visible = false;
        _objectPool.Enqueue(obj);
    }
    
    // 控制所有物体的移动状态
    public void StartAllMoving()
    {
        foreach (Node3D obj in GetChildren())
        {
            if (obj.Visible)
            {
                obj.SetMeta("IsMoving", true);
            }
        }
    }
    
    public void StopAllMoving()
    {
        foreach (Node3D obj in GetChildren())
        {
            if (obj.Visible)
            {
                obj.SetMeta("IsMoving", false);
            }
        }
    }
    
    // 重置所有物体
    public void ResetAllObjects()
    {
        foreach (Node3D obj in GetChildren())
        {
            if (obj.Visible)
            {
                obj.Position = _spawnPosition;
                obj.SetMeta("IsMoving", true);
            }
        }
    }
}
