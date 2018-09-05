using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrchardCore.ContentManagement
{
    public interface IContentPickerResultProvider
    {
        string Name { get; }
        Task<IEnumerable<ContentPickerResult>> Search(ContentPickerSearchContext searchContext);
    }

    public class ContentPickerSearchContext
    {
        public string Query { get; set; }
        public string IndexName { get; set; }
        public IEnumerable<string> ContentTypes { get; set; }
    }

    public class ContentPickerResult
    {
        public string DisplayText { get; set; }
        public string ContentItemId { get; set; }
    }
}