using EsportsTrainer.Core;

namespace EsportsTrainer.UI
{
    /// <summary>功能 Tab 契约（L4 编排：ModMenu 据此路由与渲染）。</summary>
    public interface ITab
    {
        string Id { get; }
        string Title { get; }
        Theme.AccentColor Accent { get; }
        void Render();
    }
}
