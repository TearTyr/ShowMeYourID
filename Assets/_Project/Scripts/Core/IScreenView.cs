namespace Hollowbrook.Core
{
    /// <summary>Uniform contract every screen/view implements so the bootstrap can wire
    /// them without knowing their internals.</summary>
    public interface IScreenView
    {
        void Init(GameRoot root);
        void Show();
        void Hide();
    }
}
