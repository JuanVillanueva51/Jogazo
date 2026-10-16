using Godot;
using System;

public partial class LoadScreen : Node
{
	[Export] public string NextScene = "";
	[Export] public float MinDisplayTime = 3f; //segundos minimos
	[Export] public bool WaitForVideo = true; //esperar a que termine el video
	
	private VideoStreamPlayer _video;
	private float _timer = 0f;
	private bool _videoFinished = false;
	private bool _transitioning = false;
	
	public override void _Ready(){
		_video = GetNodeOrNull<VideoStreamPlayer>("VideoStreamPlayer");
		
		if (_video != null && WaitForVideo){
			_video.Finished += OnVideoFinished;
			_video.Play();
		}else{
			_videoFinished = true;
		}
	}
	
	public override void _Process(double delta){
		_timer += (float)delta;
		
		bool timerDone = _timer >= MinDisplayTime;
		bool videoDone = !WaitForVideo || _videoFinished;
		
		if (timerDone && videoDone && !_transitioning){
			_transitioning = true;
			GoToNextScene();
		}
	}
	
	private void OnVideoFinished()
	{
		_videoFinished = true;
	}
	
	private void GoToNextScene()
	{
		if (string.IsNullOrEmpty(NextScene))
		{
			GD.PrintErr("LoadScreen: NextScene no está asignado en el Inspector.");
			return;
		}
		GetTree().ChangeSceneToFile(NextScene);
	}
}
