using ElmahCore;

namespace SampleProject.Interface.Elmah;

public class CmsErrorLogFilter:IErrorFilter
{
    public void OnErrorModuleFiltering(object sender, ExceptionFilterEventArgs args)
    {
        //查無檔案類例外不紀錄
        if (args.Exception.GetBaseException() is FileNotFoundException)
        {
            args.Dismiss(); 
        }

        //404時不紀錄
        if (args.Context is HttpContext { Response.StatusCode: 404 })
        {
            args.Dismiss();
        }
         
    }
}