
namespace FPS.Manager.Game
{
	public class InputManager
	{
		private InputActions inputActions;
		public InputActions IA;

		public void Init()
		{
			inputActions = new InputActions();
			inputActions.Enable();
		}
	}
}
