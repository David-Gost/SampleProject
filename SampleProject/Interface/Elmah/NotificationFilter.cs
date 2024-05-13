using System.Diagnostics;
using ElmahCore;

namespace SampleProject.Interface.Elmah;

public class NotificationFilter:IErrorNotifier
{
    public void Notify(Error error)
    {
        Debug.WriteLine(error.Message);
    }

    public string Name { get; }
}