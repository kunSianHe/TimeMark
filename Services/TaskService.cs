using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FocusTimer.Models;

namespace FocusTimer.Services
{
    public class TaskService
    {
        private readonly DatabaseManager _db;
        private const string TASKS_TABLE = "tasks";
        private const string TODO_TASKS_TABLE = "todo_tasks";

        public TaskService()
        {
            _db = new DatabaseManager();
        }

        public async Task<int> SaveTaskRecord(Models.FocusTask task)
        {
            return await _db.InsertAsync(TASKS_TABLE, task);
        }

        public async Task<IEnumerable<Models.FocusTask>> GetTasksAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            return await _db.QueryByDateRangeAsync<Models.FocusTask>(
                TASKS_TABLE,
                "StartTime",
                startDate,
                endDate);
        }

        public async Task<int> SaveTodoTask(Models.FocusTask task)
        {
            return await _db.InsertAsync(TODO_TASKS_TABLE, task);
        }

        public async Task<IEnumerable<Models.FocusTask>> GetTodoTasksAsync()
        {
            return await _db.QueryAsync<Models.FocusTask>(
                $"SELECT * FROM {TODO_TASKS_TABLE} ORDER BY created_at DESC");
        }

        public async Task<bool> DeleteTodoTask(int id)
        {
            return await _db.DeleteAsync(TODO_TASKS_TABLE, id);
        }
    }
} 