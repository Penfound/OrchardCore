using OrchardCore.ContentManagement;

namespace OrchardCore.ContentFields.Fields
{
    public class MultiSelectField : ContentField
    {
        public string[] Values { get; set; }
    }
}