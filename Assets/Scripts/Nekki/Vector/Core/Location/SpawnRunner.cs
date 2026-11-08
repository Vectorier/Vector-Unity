using Nekki.Vector.Core.Animation;

namespace Nekki.Vector.Core.Location
{
	public class SpawnRunner : Runner
	{
		protected string _animations;

		public AnimationReaction Reaction;

		public SpawnRunner()
			: base(0f, 0f)
		{
			_TypeClass = RunnerType.Spawn;
		}

		public SpawnRunner(float x, float y, string n, string animation)
			: base(x, y)
		{
			_animations = animation;
			_TypeClass = RunnerType.Spawn;
			_Name = n;
			_Hash = n.GetHashCode();
			if (string.IsNullOrEmpty(_animations))
			{
				return;	
			}
			Reaction = AnimationLoader.ParseReaction(_animations.Split('|'));
		}

		public override bool Render()
		{
			return true;
		}
	}
}
