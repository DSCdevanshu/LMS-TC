using System.Data;

namespace TCBackend.Services.IServices
{
    /// <summary>
    /// Converts a collection of model objects into a <see cref="DataTable"/>,
    /// primarily for passing table-valued parameters (TVPs) to stored procedures
    /// (e.g. via Dapper's <c>AsTableValuedParameter</c>).
    /// </summary>
    public interface IDataTableService
    {
        /// <summary>
        /// Builds a <see cref="DataTable"/> from <paramref name="items"/> using the public,
        /// readable instance properties of <typeparamref name="T"/>.
        /// <para>
        /// Columns are produced in property-declaration order. When the result is used as a
        /// SQL Server TVP, the model's property order must therefore match the SQL type's
        /// column order. A <see langword="null"/> collection yields a correctly-shaped,
        /// empty table (zero rows).
        /// </para>
        /// </summary>
        /// <typeparam name="T">The model type that defines the table columns.</typeparam>
        /// <param name="items">The source objects (rows). May be <see langword="null"/>.</param>
        DataTable ToDataTable<T>(IEnumerable<T>? items);
    }
}
