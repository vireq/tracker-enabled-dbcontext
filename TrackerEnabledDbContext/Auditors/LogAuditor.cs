using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TrackerEnabledDbContext.Common.Auditors.Helpers;
using TrackerEnabledDbContext.Common.Configuration;
using TrackerEnabledDbContext.Common.Models;
using TrackerEnabledDbContext.Core.Common.Configuration;
using TrackerEnabledDbContext.Core.Common.Interfaces;

namespace TrackerEnabledDbContext.Core.Common.Auditors
{
    internal class LogAuditor : IDisposable
    {
        private readonly EntityEntry _dbEntry;
        private readonly DbEntryValuesWrapper _dbEntryValuesWrapper;

        internal LogAuditor(EntityEntry dbEntry)
        {
            _dbEntry = dbEntry;
            _dbEntryValuesWrapper = new DbEntryValuesWrapper(_dbEntry);
        }

        public void Dispose()
        {
        }

        internal AuditLog CreateLogRecord(object userName, EventType eventType, ITrackerContext context, ExpandoObject metadata)
        {
            Type entityType = _dbEntry.Entity.GetType();

            if (!EntityTrackingConfiguration.IsTrackingEnabled(entityType))
            {
                return null;
            }

            DateTime changeTime = DateTime.UtcNow;

            //changed to static class by Aaron Sulwer 3/16/2018
            List<PropertyConfigurationKey> keyNames = (context as DbContext).GetKeyNames(entityType).ToList();

            var newlog = new AuditLog
            {
                UserName = userName?.ToString(),
                EventDateUTC = changeTime,
                EventType = eventType,
                TypeFullName = entityType.FullName,
                RecordId = GetPrimaryKeyValuesOf(_dbEntry, keyNames).ToString()
            };

            var logMetadata = metadata
                .Where(x => x.Value != null)
                .Select(m => new LogMetadata
                {
                    AuditLog = newlog,
                    Key = m.Key,
                    Value = m.Value?.ToString()
                })
            .ToList();

            newlog.Metadata = logMetadata;

            var detailsAuditor = GetDetailsAuditor(eventType, newlog);

            newlog.LogDetails = detailsAuditor.CreateLogDetails().ToList();

            if (newlog.LogDetails.Any())
                return newlog;
            else
                return null;
        }

        private ChangeLogDetailsAuditor GetDetailsAuditor(EventType eventType, AuditLog newlog)
        {
            switch (eventType)
            {
                case EventType.Added:
                    return new AdditionLogDetailsAuditor(_dbEntry, newlog);

                case EventType.Deleted:
                    return new DeletionLogDetailsAuditor(_dbEntry, newlog, _dbEntryValuesWrapper);

                case EventType.Modified:
                    return new ChangeLogDetailsAuditor(_dbEntry, newlog, _dbEntryValuesWrapper);

                case EventType.SoftDeleted:
                    return new SoftDeletedLogDetailsAuditor(_dbEntry, newlog, _dbEntryValuesWrapper);

                case EventType.UnDeleted:
                    return new UnDeletedLogDetailsAuditor(_dbEntry, newlog, _dbEntryValuesWrapper);

                default:
                    return null;
            }
        }

        private object GetPrimaryKeyValuesOf(
            EntityEntry dbEntry,
            List<PropertyConfigurationKey> properties)
        {
            if (properties.Count == 0)
            {
                throw new KeyNotFoundException("key not found for " + dbEntry.Entity.GetType().FullName);
            }

            if (properties.Count == 1)
            {
                return _dbEntryValuesWrapper.OriginalValue(properties.First().PropertyName);
            }

            string output = "[";

            output += string.Join(",",
                properties.Select(colName => _dbEntryValuesWrapper.OriginalValue(colName.PropertyName)));

            output += "]";
            return output;
        }
    }
}