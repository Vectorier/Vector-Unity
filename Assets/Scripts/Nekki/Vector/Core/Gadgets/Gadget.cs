namespace Nekki.Vector.Core.Gadgets
{
	public abstract class Gadget
	{
		private GadgetType _gadgetType;

		public GadgetType gadgetType => _gadgetType;

		protected Gadget(GadgetType gadgetType)
		{
			_gadgetType = gadgetType;
		}

		public abstract bool IsCanUse();

		public virtual void Play()
		{
		}

		public virtual void Stop()
		{
        }
    }
}
