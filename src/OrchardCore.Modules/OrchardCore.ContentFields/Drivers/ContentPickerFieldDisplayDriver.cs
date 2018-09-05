using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentFields.Settings;
using OrchardCore.ContentFields.ViewModels;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.ContentManagement.Records;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using YesSql;
using YesSql.Services;

namespace OrchardCore.ContentFields.Fields
{
    public class ContentPickerFieldDisplayDriver : ContentFieldDisplayDriver<ContentPickerField>
    {
        private readonly IContentManager _contentManager;
        private readonly ISession _session;

        public ContentPickerFieldDisplayDriver(
            IContentManager contentManager,
            ISession session,
            IStringLocalizer<ContentPickerFieldDisplayDriver> localizer)
        {
            _contentManager = contentManager;
            _session = session;
            T = localizer;
        }

        public IStringLocalizer T { get; set; }

        public override IDisplayResult Display(ContentPickerField field, BuildFieldDisplayContext context)
        {
            return Initialize<DisplayContentPickerFieldViewModel>("ContentPickerField", async model =>
            {
                model.Field = field;
                model.Part = context.ContentPart;
                model.PartFieldDefinition = context.PartFieldDefinition;
                model.Updater = context.Updater;
                model.SelectedContentItemIds = string.Join(",", field.ContentItemIds);
                model.ContentItems = await _session.Query<ContentItem, ContentItemIndex>()
                    .With<ContentItemIndex>(x => x.ContentItemId.IsIn(field.ContentItemIds) && x.Published)
                    .ListAsync();
            })
            .Location("Content")
            .Location("SummaryAdmin", "");
        }

        public override IDisplayResult Edit(ContentPickerField field, BuildFieldEditorContext context)
        {
            return Initialize<EditContentPickerFieldViewModel>(GetEditorShapeType(context), async model =>
            {
                model.ContentItemIds = field.ContentItemIds == null ? string.Empty : string.Join(",", field.ContentItemIds);

                model.Field = field;
                model.Part = context.ContentPart;
                model.PartFieldDefinition = context.PartFieldDefinition;

                model.SelectedItems = new List<ContentPickerItemViewModel>();

                foreach (var contentItemId in field.ContentItemIds ?? new string[0])
                {
                    var contentItem = await _contentManager.GetAsync(contentItemId);
                    var contentItemMetadata = await _contentManager.PopulateAspectAsync<ContentItemMetadata>(contentItem);
                    model.SelectedItems.Add(new ContentPickerItemViewModel
                    {
                        ContentItemId = contentItemId,
                        DisplayText = contentItemMetadata.DisplayText
                    });
                }
            });
        }

        public override async Task<IDisplayResult> UpdateAsync(ContentPickerField field, IUpdateModel updater, UpdateFieldEditorContext context)
        {
            var viewModel = new EditContentPickerFieldViewModel();

            var modelUpdated = await updater.TryUpdateModelAsync(viewModel, Prefix, f => f.ContentItemIds);

            if (!modelUpdated)
            {
                return Edit(field, context);
            }

            field.ContentItemIds = viewModel.ContentItemIds == null
                ? new string[0] : viewModel.ContentItemIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var settings = context.PartFieldDefinition.Settings.ToObject<ContentPickerFieldSettings>();

            if (settings.Required && field.ContentItemIds.Length == 0)
            {
                updater.ModelState.AddModelError(Prefix, T[$"The {context.PartFieldDefinition.DisplayName()} field is required."]);
            }

            if (!settings.Multiple && field.ContentItemIds.Length > 1)
            {
                updater.ModelState.AddModelError(Prefix, T[$"The {context.PartFieldDefinition.DisplayName()} field cannot contain multiple items."]);
            }

            return Edit(field, context);
        }
    }
}