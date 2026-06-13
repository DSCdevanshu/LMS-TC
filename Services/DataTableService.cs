using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using TCBackend.Services.IServices;

namespace TCBackend.Services
{
    /// <summary>
    /// Reflection-based <see cref="IDataTableService"/>. Property metadata is computed once
    /// per model type and cached, so repeated conversions are cheap.
    /// </summary>
    public class DataTableService : IDataTableService
    {
        private static readonly ConcurrentDictionary<Type, ColumnMap[]> _columnCache = new();

        public DataTable ToDataTable<T>(IEnumerable<T>? items)
        {
            var columns = GetColumns(typeof(T));
            var table = new DataTable(typeof(T).Name);

            foreach (var column in columns)
            {
                table.Columns.Add(new DataColumn(column.Property.Name, column.ColumnType)
                {
                    AllowDBNull = column.AllowDBNull
                });
            }

            if (items is null)
            {
                return table;
            }

            foreach (var item in items)
            {
                if (item is null)
                {
                    continue;
                }

                var values = new object[columns.Length];
                for (int i = 0; i < columns.Length; i++)
                {
                    var value = columns[i].Property.GetValue(item);
                    values[i] = value is null
                        ? DBNull.Value
                        : columns[i].IsEnum
                            ? Convert.ChangeType(value, columns[i].ColumnType)
                            : value;
                }

                table.Rows.Add(values);
            }

            return table;
        }

        private static ColumnMap[] GetColumns(Type type) =>
            _columnCache.GetOrAdd(type, static t => t
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Select(CreateColumn)
                .ToArray());

        private static ColumnMap CreateColumn(PropertyInfo property)
        {
            var propertyType = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            var isEnum = underlyingType.IsEnum;

            // DataColumn cannot use Nullable<T>; enums are stored as their numeric base
            // type so they map cleanly onto SQL TVP columns.
            var columnType = isEnum ? Enum.GetUnderlyingType(underlyingType) : underlyingType;
            var allowDBNull = !propertyType.IsValueType
                              || Nullable.GetUnderlyingType(propertyType) is not null;

            return new ColumnMap(property, columnType, allowDBNull, isEnum);
        }

        private sealed record ColumnMap(PropertyInfo Property, Type ColumnType, bool AllowDBNull, bool IsEnum);
    }
}
