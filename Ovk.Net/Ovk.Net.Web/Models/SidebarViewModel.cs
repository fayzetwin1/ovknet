using Ovk.Net.Core.Models;

namespace Ovk.Net.Web.Models;

public class SidebarViewModel
{
    public Profile? Profile { get; set; }
    public int UnreadMessages { get; set; }
}
