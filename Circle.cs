using Godot;
using System;

public partial class Circle : Node2D
{
	Vector2 Center = new Vector2(DisplayServer.WindowGetSize().X / 2, DisplayServer.WindowGetSize().Y / 2);
	float Radius = 300.0f;
	float StartAngle = float.Pi * 7 / 8;
	float EndAngle = float.Pi * 9 / 8;
	float LineWidth = 11.0f;
	
	public override void _Input(InputEvent @event)
	{
		if (typeof(InputEventMouseButton) == @event.GetType())
		{
			if (((InputEventMouseButton)@event).Pressed && MouseButton.Left == ((InputEventMouseButton)@event).ButtonIndex)
			{
				if (IsPointOnArc(((InputEventMouseButton)@event).Position))
				{
					var camera = GetViewport().GetCamera3D();
					var nodes = GetNode<InfiniteSpawner>("../../InfiniteSpawner").ActiveObject;
					foreach (var node in nodes)
					{
						if (IsPointOnArc(camera.UnprojectPosition(node.GlobalPosition), 50.0f))
						{
							GD.Print("Success!");
						}
					}
					GD.Print("Clicked!");
				}
			}
		}
	}

	public bool IsPointOnArc(Vector2 point, float tolerance = 5.0f)
	{
		var vec = point - Center;
		var distance = vec.Length();

		if (distance < Radius - LineWidth / 2 - tolerance || distance > Radius + LineWidth / 2 + tolerance)
		{
			return false;
		}

		var angle = vec.Angle();
		if (angle < 0)
		{
			angle += 2 * float.Pi;
		}

		var adjustedStart = StartAngle % float.Tau;
		var adjustedEnd = EndAngle % float.Tau;

		if (adjustedStart <= adjustedEnd)
		{
			return angle >= adjustedStart && angle <= adjustedEnd;
		}
		else
		{
			return angle >= adjustedStart || angle <= adjustedEnd;
		}
		
	}

	public override void _Draw()
	{
		DrawArc(Center, Radius, 0, float.Tau, 64, new Color(1, 1, 1), 7, true);
		DrawArc(Center, Radius, StartAngle, EndAngle, 32, new Color(1, 0.95f, 0), LineWidth, true);
    }


}
