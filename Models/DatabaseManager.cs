using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Dapper;
using System.Linq;

namespace FocusTimer.Services
{
    public class DatabaseManager
    {
        private readonly string connectionString;

        public DatabaseManager()
        {
            connectionString = "Server=localhost;Database=focus_timer;Uid=你的用戶名;Pwd=你的密碼;";
        }

        // 通用查詢方法
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            return await connection.QueryAsync<T>(sql, param);
        }

        // 通用執行方法（返回影響的行數）
        public async Task<int> ExecuteAsync(string sql, object param = null)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            return await connection.ExecuteAsync(sql, param);
        }

        // 通用插入方法（返回新插入的 ID）
        public async Task<int> InsertAsync<T>(string tableName, T entity)
        {
            var properties = typeof(T).GetProperties()
                .Where(p => p.Name.ToLower() != "id" && p.CanRead)
                .ToList();

            var columns = string.Join(", ", properties.Select(p => p.Name.ToLower()));
            var parameters = string.Join(", ", properties.Select(p => "@" + p.Name));

            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}); SELECT LAST_INSERT_ID();";

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            return await connection.ExecuteScalarAsync<int>(sql, entity);
        }

        // 通用刪除方法
        public async Task<bool> DeleteAsync(string tableName, int id)
        {
            var sql = $"DELETE FROM {tableName} WHERE id = @Id";
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            var result = await connection.ExecuteAsync(sql, new { Id = id });
            return result > 0;
        }

        // 修改日期範圍查詢方法
        public async Task<IEnumerable<T>> QueryByDateRangeAsync<T>(
            string tableName, 
            string dateField,
            DateTime? startDate = null, 
            DateTime? endDate = null,
            string additionalWhere = null)
        {
            var sql = new System.Text.StringBuilder($@"
                SELECT 
                    id,
                    title,
                    type,
                    notes,
                    DATE_FORMAT(start_time, '%Y-%m-%d %H:%i:%s') as start_time,
                    DATE_FORMAT(end_time, '%Y-%m-%d %H:%i:%s') as end_time,
                    duration,
                    is_completed
                FROM {tableName} 
                WHERE 1=1");
            
            var parameters = new DynamicParameters();

            if (startDate.HasValue)
            {
                sql.Append($" AND DATE({dateField}) >= @StartDate");
                parameters.Add("StartDate", startDate.Value.ToString("yyyy-MM-dd"));
            }
            if (endDate.HasValue)
            {
                sql.Append($" AND DATE({dateField}) <= @EndDate");
                parameters.Add("EndDate", endDate.Value.ToString("yyyy-MM-dd"));
            }
            if (!string.IsNullOrEmpty(additionalWhere))
            {
                sql.Append($" AND {additionalWhere}");
            }

            sql.Append($" ORDER BY {dateField} DESC");

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            
            // 直接使用 Dapper 的泛型查詢
            var result = await connection.QueryAsync<T>(sql.ToString(), parameters);
            return result;
        }

        private T MapToType<T>(dynamic row)
        {
            var result = Activator.CreateInstance<T>();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                try
                {
                    var value = ((IDictionary<string, object>)row)[prop.Name.ToLower()];
                    if (value != null)
                    {
                        // 特殊處理日期時間類型
                        if (prop.PropertyType == typeof(DateTime) && value is string dateStr)
                        {
                            prop.SetValue(result, DateTime.Parse(dateStr));
                        }
                        // 特殊處理可空日期時間類型
                        else if (prop.PropertyType == typeof(DateTime?) && value is string nullableDateStr)
                        {
                            prop.SetValue(result, DateTime.Parse(nullableDateStr));
                        }
                        else
                        {
                            prop.SetValue(result, Convert.ChangeType(value, prop.PropertyType));
                        }
                    }
                }
                catch { /* 忽略轉換失敗的欄位 */ }
            }

            return result;
        }
    }
} 