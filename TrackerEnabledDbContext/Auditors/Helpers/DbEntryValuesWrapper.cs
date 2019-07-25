using Microsoft.EntityFrameworkCore.ChangeTracking;
using TrackerEnabledDbContext.Common.Configuration;

namespace TrackerEnabledDbContext.Common.Auditors.Helpers
{
    public class DbEntryValuesWrapper
    {
        protected readonly EntityEntry _dbEntry;
        private PropertyValues _entryValues = null;

        private PropertyValues EntryPropertyValues => _entryValues ?? (_entryValues = _dbEntry.GetDatabaseValues());

        public DbEntryValuesWrapper(EntityEntry dbEntry)
        {
            _dbEntry = dbEntry;
        }

        public object OriginalValue(string propertyName)
        {
            object originalValue = null;

            if (GlobalTrackingConfig.DisconnectedContext)
            {
                originalValue = EntryPropertyValues.GetValue<object>(propertyName);
            }
            else
            {
                originalValue = _dbEntry.Property(propertyName).OriginalValue;
            }

            return originalValue;
        }
    }
}