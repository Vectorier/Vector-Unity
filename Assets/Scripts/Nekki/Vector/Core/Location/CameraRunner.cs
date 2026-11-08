
namespace Nekki.Vector.Core.Location
{
	public class CameraRunner : SpawnRunner
	{
		public CameraRunner(float x, float y, string n, string animation) : base(x, y, n, animation)
		{
			_TypeClass = RunnerType.Camera;
		}

	}
}
