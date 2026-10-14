using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CopyExtension.UI
{
    public interface IOptionGui
    {
        T? GetChoiceResult<T>(string text, string details, Dictionary<string, T> Options);
    }
}
