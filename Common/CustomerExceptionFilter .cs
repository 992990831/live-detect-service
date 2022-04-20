using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace LiveDetect.Service.Common
{
    public class CustomExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            
            // 设置为true，表示异常已经被处理了
            context.ExceptionHandled = true;
        }
    }
}
