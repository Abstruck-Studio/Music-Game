using Godot;
using System;

public partial class Camera3d : Camera3D
{
// 精确传感器处理
    private float _deviceRoll = 0.0f;
    private float _targetRoll = 0.0f;
    private float _baseGravityRoll = 0.0f;
    
    // 传感器状态
    private Vector3 _lastGyro = Vector3.Zero;
    private bool _gyroInitialized = false;
    private Vector3 _gravityHistory = Vector3.Zero;
    
    // 调谐参数
    [Export] public float GyroSensitivity = 1.0f;
    [Export] public float GravityInfluence = 0.05f;
    [Export] public float MaxRotationSpeed = Mathf.Pi * 3.0f;
    
    // 性能优化
    private ulong _lastUpdateTime = 0;
    
    public override void _Ready()
    {
        // 初始校准
        ResetBaseOrientation();
        
        GD.Print("CameraSync initialized. Waiting for calibration...");
    }
    
    public void ResetBaseOrientation()
    {
        Vector3 gravity = Input.GetGravity();
        if (gravity.Length() > 0.1f)
        {
            _baseGravityRoll = CalculateGravityRoll(gravity);
            _deviceRoll = _baseGravityRoll;
            _targetRoll = _baseGravityRoll;
            Rotation = new Vector3(Rotation.X, Rotation.Y, _deviceRoll);
        }
        else
        {
            GD.PrintErr("Cannot reset: No gravity data available");
        }
        
        _gyroInitialized = false;
        _lastGyro = Vector3.Zero;
        _gravityHistory = Vector3.Zero;
    }

    public override void _PhysicsProcess(double delta)
    {
        // 获取当前时间
        ulong currentTime = Time.GetTicksUsec();
        float dt = (_lastUpdateTime > 0) ? 
            (currentTime - _lastUpdateTime) * 0.000001f : 
            (float)delta;
        _lastUpdateTime = currentTime;
        
        // 确保有最小时间增量
        dt = Mathf.Max(dt, 0.001f);
        
        // 更新旋转
        UpdateRotation(dt);
    }
    
    private void UpdateRotation(float dt)
    {
        // 1. 获取传感器数据
        Vector3 gyro = Input.GetGyroscope();
        Vector3 gravity = Input.GetGravity();
        
        // 2. 初始化陀螺仪
        if (!_gyroInitialized && gravity.Length() > 0.2f)
        {
            _lastGyro = gyro;
            _gyroInitialized = true;
            GD.Print("Gyro initialized");
            return;
        }
        
        // 3. 计算当前重力滚转
        float currentGravityRoll = CalculateGravityRoll(gravity);
        
        // 4. 处理陀螺仪数据
        if (_gyroInitialized)
        {
            // 计算陀螺仪差异
            float deltaGyroZ = gyro.Z - _lastGyro.Z;
            
            // 限制最大旋转速度
            deltaGyroZ = Mathf.Clamp(deltaGyroZ, -MaxRotationSpeed, MaxRotationSpeed);
            
            // 直接应用变化量
            _targetRoll += deltaGyroZ * dt * GyroSensitivity;
            
            // 保存当前陀螺仪值
            _lastGyro = gyro;
        }
        
        // 5. 重力辅助校正
        float gravityDifference = Mathf.AngleDifference(currentGravityRoll, _baseGravityRoll);
        float targetGravityRoll = _baseGravityRoll + gravityDifference;
        
        // 混合重力校正和陀螺仪数据
        _targetRoll = Mathf.LerpAngle(_targetRoll, targetGravityRoll, GravityInfluence);
        
        // 6. 直接应用目标旋转
        _deviceRoll = _targetRoll;
        
        // 7. 应用到相机
        Vector3 newRotation = Rotation;
        newRotation.Z = _deviceRoll;
        Rotation = newRotation;
    }
    
    private float CalculateGravityRoll(Vector3 gravity)
    {
        // 稳定重力数据
        _gravityHistory = _gravityHistory * 0.7f + gravity * 0.3f;
        Vector3 stableGravity = _gravityHistory.Normalized();
        
        // 设备水平放置时
        if (Mathf.Abs(stableGravity.Z) > 0.9f)
        {
            // Gravity: (0, 0, -9.8) 当设备平放屏幕朝上
            return Mathf.Atan2(-stableGravity.X, -stableGravity.Y);
        }
        
        // 设备竖直放置时
        if (Mathf.Abs(stableGravity.Y) > 0.9f)
        {
            return Mathf.Atan2(stableGravity.X, stableGravity.Z);
        }
        
        // 其他角度 - 使用设备XY平面投影
        Vector3 horizontalProjection = new Vector3(stableGravity.X, stableGravity.Y, 0).Normalized();
        return Mathf.Atan2(-horizontalProjection.Y, horizontalProjection.X);
    }
}
