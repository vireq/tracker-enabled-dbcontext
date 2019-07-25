using Microsoft.EntityFrameworkCore.ChangeTracking;
using TrackerEnabledDbContext.Common.Auditors.Comparators;
using TrackerEnabledDbContext.Common.Auditors.Helpers;
using TrackerEnabledDbContext.Common.Configuration;
using TrackerEnabledDbContext.Common.Extensions;
using TrackerEnabledDbContext.Common.Models;

namespace TrackerEnabledDbContext.Core.Common.Auditors
{
    public class DeletionLogDetailsAuditor: ChangeLogDetailsAuditor
    {
        public DeletionLogDetailsAuditor(EntityEntry dbEntry, AuditLog log,
            DbEntryValuesWrapper dbEntryValuesWrapper) : base(dbEntry, log, dbEntryValuesWrapper)
        {
        }

        protected override bool IsValueChanged(string propertyName)
        {
            if (GlobalTrackingConfig.TrackEmptyPropertiesOnAdditionAndDeletion)
                return true;

            var propertyType = DbEntry.Entity.GetType().GetProperty(propertyName).PropertyType;
            object defaultValue = propertyType.DefaultValue();
            object originalValue = OriginalValue(propertyName);

            Comparator comparator = ComparatorFactory.GetComparator(propertyType);

            return !comparator.AreEqual(defaultValue, originalValue);
        }

        protected override object CurrentValue(string propertyName)
        {
            return null;
        }
    }
}
