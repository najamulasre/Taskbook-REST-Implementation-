﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Najam.TaskBook.Data;
using Najam.TaskBook.Domain;
using Task = Najam.TaskBook.Domain.Task;


namespace Najam.TaskBook.Business
{
    public class TaskBookBusiness : ITaskBookBusiness
    {
        private readonly TaskBookDbContext _dbContext;

        public TaskBookBusiness(TaskBookDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<DateTime> GetServerDateTime()
        {
            using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "select getdate()";
                _dbContext.Database.OpenConnection();
                object result = await command.ExecuteScalarAsync();

                return (DateTime)result;
            }
        }

        public Task<UserGroup[]> GetUserGroupsByUserId(Guid userId)
        {
            IQueryable<UserGroup> query = _dbContext.UserGroups
                .Include(ug => ug.Group)
                .Where(ug => ug.UserId == userId && ug.RelationType == UserGroupRelationType.Owner);

            return query.ToArrayAsync();
        }

        public Task<UserGroup> GetUserGroupByGroupId(Guid userId, Guid groupId)
        {
            IQueryable<UserGroup> query = _dbContext.UserGroups
                .Include(ug => ug.Group)
                .Where(ug => ug.UserId == userId && ug.GroupId == groupId && ug.RelationType == UserGroupRelationType.Owner);

            return query.SingleOrDefaultAsync();
        }

        public async Task<UserGroup> CreateUserGroup(Guid userId, string groupName, bool isActive)
        {
            // add group
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = groupName,
                IsActive = isActive
            };

            // add relationship with user
            var userGroup = new UserGroup
            {
                UserId = userId,
                Group = group,
                RelationType = UserGroupRelationType.Owner
            };

            _dbContext.UserGroups.Add(userGroup);
            _dbContext.Groups.Add(group);

            await _dbContext.SaveChangesAsync();

            return await GetUserGroupByGroupId(userId, group.Id);
        }

        public System.Threading.Tasks.Task DeleteGroup(Guid groupId)
        {
            Group group = _dbContext.Groups.Single(g => g.Id == groupId);
            _dbContext.Groups.Remove(group);

            return _dbContext.SaveChangesAsync();
        }

        public async Task<UserGroup> UpdateGroup(Guid userId, Guid groupId, string groupName, bool isActive)
        {
            Group group = _dbContext.Groups.Single(g => g.Id == groupId);

            group.Name = groupName;
            group.IsActive = isActive;

            await _dbContext.SaveChangesAsync();

            return await GetUserGroupByGroupId(userId, group.Id);
        }

        public Task<bool> IsUserGroupOwner(Guid userId, Guid groupId)
        {
            IQueryable<UserGroup> query = _dbContext.UserGroups
                .Where(ug => ug.UserId == userId && ug.GroupId == groupId && ug.RelationType == UserGroupRelationType.Owner);

            return query.AnyAsync();
        }

        public Task<UserGroup[]> GetGroupMemberships(Guid groupId)
        {
            IQueryable<UserGroup> query = _dbContext.UserGroups
                .Include(ug => ug.Group)
                .Include(ug => ug.User)
                .Where(ug => ug.GroupId == groupId && ug.RelationType == UserGroupRelationType.Member);

            return query.ToArrayAsync();
        }

        public Task<UserGroup> GetGroupMembership(Guid userId, Guid groupId)
        {
            return _dbContext.UserGroups
                .Include(ug => ug.Group)
                .Include(ug => ug.User)
                .SingleOrDefaultAsync(ug => ug.GroupId == groupId && ug.UserId == userId);
        }

        public async Task<UserGroup> CrateGroupMembership(Guid userId, Guid groupId)
        {
            var userGroup = new UserGroup
            {
                UserId = userId,
                GroupId = groupId,
                RelationType = UserGroupRelationType.Member
            };

            _dbContext.UserGroups.Add(userGroup);
            await _dbContext.SaveChangesAsync();

            return await GetGroupMembership(userId, groupId);
        }

        public async System.Threading.Tasks.Task DeleteGroupMembership(Guid userId, Guid groupId)
        {
            UserGroup group = await GetGroupMembership(userId, groupId);

            if (group == null)
                return;

            _dbContext.UserGroups.Remove(group);
            await _dbContext.SaveChangesAsync();
        }

        public Task<UserGroup[]> GetUserMemberships(Guid userId)
        {
            IQueryable<UserGroup> query = _dbContext.UserGroups
                .Include(ug => ug.User)
                .Include(ug => ug.Group)
                .Where(ug => ug.UserId == userId);

            return query.ToArrayAsync();
        }

        public Task<bool> IsUserRelatedWithGroup(Guid userId, Guid groupId)
        {
            return _dbContext.UserGroups
                .AnyAsync(ug => ug.UserId == userId && ug.GroupId == groupId);
        }

        public Task<Task[]> GetTasksByGroupId(Guid groupId)
        {
            IQueryable<Task> query = _dbContext.Tasks
                .Include(t => t.Group)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .Where(t => t.GroupId == groupId);

            return query.ToArrayAsync();
        }

        public Task<Task> GetTaskByTaskId(Guid taskId)
        {
            IQueryable<Task> query = _dbContext.Tasks
                .Include(t => t.Group)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .Where(t => t.Id == taskId);

            return query.SingleOrDefaultAsync();
        }

        public async Task<Task> CreateGroupTask(Guid groupId, string title, string description, DateTime deadline, Guid createdByUserId)
        {
            var task = new Task
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                Title = title,
                Description = description,
                Deadline = deadline,
                CreatedByUserId = createdByUserId
            };

            _dbContext.Tasks.Add(task);
            await _dbContext.SaveChangesAsync();

            return await GetTaskByTaskId(task.Id);
        }

        public Task<bool> IsUserTaskCreator(Guid userId, Guid taskId)
        {
            return _dbContext.Tasks.AnyAsync(t => t.Id == taskId && t.CreatedByUserId == userId);
        }

        public async Task<Task> UpdateGroupTask(Guid taskId, string title, string description, DateTime deadline)
        {
            Task task = await _dbContext.Tasks.FindAsync(taskId);

            if (task == null)
                return null;

            task.Title = title;
            task.Description = description;
            task.Deadline = deadline;

            await _dbContext.SaveChangesAsync();

            return task;
        }

        public async Task<bool> DeleteTask(Guid taskId)
        {
            Task task = await _dbContext.Tasks.FindAsync(taskId);

            if (task == null)
                return false;

            _dbContext.Tasks.Remove(task);

            await _dbContext.SaveChangesAsync();

            return true;
        }

        public Task<Task[]> GetUsersTaskByUserId(Guid userId)
        {
            IQueryable<Task> query = _dbContext.UserGroups
                .Where(g => g.UserId == userId)
                .SelectMany(g => g.Group.Tasks)
                .Include(t => t.Group)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser);

            return query.ToArrayAsync();
        }

        public Task<Task> GetUsersTaskByUserAndTaskId(Guid userId, Guid taskId)
        {
            IQueryable<Task> query = _dbContext.UserGroups
                .Where(g => g.UserId == userId)
                .SelectMany(g => g.Group.Tasks)
                .Where(t => t.Id == taskId)
                .Include(t => t.Group)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser);

            return query.SingleOrDefaultAsync();
        }

        public Task<Task[]> GetUsersTaskAssignmentsByUserId(Guid userId)
        {
            IQueryable<Task> query = _dbContext.UserGroups
                .Where(g => g.UserId == userId)
                .SelectMany(g => g.Group.Tasks)
                .Include(t => t.Group)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .Where(t => t.AssignedToUserId.HasValue && !t.DateTimeCompleted.HasValue);

            return query.ToArrayAsync();
        }

        public Task<Task> GetUsersTaskAssignmentByUserAndTaskId(Guid userId, Guid taskId)
        {
            IQueryable<Task> query = _dbContext.UserGroups
                .Where(g => g.UserId == userId)
                .SelectMany(g => g.Group.Tasks)
                .Where(t => t.Id == taskId)
                .Include(t => t.Group)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .Where(t => t.AssignedToUserId.HasValue && !t.DateTimeCompleted.HasValue);

            return query.SingleOrDefaultAsync();
        }

        public async Task<Task> AssignTask(Guid assignToUserId, Guid taskId)
        {
            Task task = _dbContext.Tasks.Find(taskId);
            task.AssignedToUserId = assignToUserId;
            task.DateTimeAssigned = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return await GetUsersTaskAssignmentByUserAndTaskId(assignToUserId, taskId);
        }

        public async Task<bool> UnassignTask(Guid taskId)
        {
            Task task = _dbContext.Tasks.Find(taskId);

            if (task == null)
                return false;

            task.AssignedToUserId = null;
            task.DateTimeAssigned = null;

            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}