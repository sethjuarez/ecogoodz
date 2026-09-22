using EcoGoodz.Data;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.StaffTask;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

[Authorize]
public class StaffTaskController : Controller
{
    private readonly EcoGoodzDbContext _context;

    public StaffTaskController(EcoGoodzDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(bool includeCompleted = false)
    {
        var currentUserId = User.GetLegacyUserId();
        var query = _context.AssignTasks
            .Where(t => t.IsActive && t.Task != null && t.Task.IsActive)
            .Include(t => t.AssignedToNavigation)
            .Include(t => t.Task)
                .ThenInclude(t => t!.CreatedByNavigation)
            .Include(t => t.TaskHeadlineNavigation)
            .AsQueryable();

        if (!includeCompleted)
        {
            query = query.Where(t => !t.IsDone);
        }

        var tasks = await query
            .OrderBy(t => t.Task!.Duedate == null)
            .ThenBy(t => t.Task!.Duedate)
            .ThenByDescending(t => t.Id)
            .Select(t => new StaffTaskListItemViewModel
            {
                Id = t.Id,
                TaskId = t.TaskId ?? 0,
                Description = t.Task!.Description ?? string.Empty,
                DueDate = t.Task.Duedate,
                AssignedToName = t.AssignedToNavigation != null
                    ? t.AssignedToNavigation.FirstName + " " + t.AssignedToNavigation.LastName
                    : null,
                Headline = t.TaskHeadlineNavigation != null ? t.TaskHeadlineNavigation.Headline : null,
                CreatedByName = t.Task.CreatedByNavigation != null
                    ? t.Task.CreatedByNavigation.FirstName + " " + t.Task.CreatedByNavigation.LastName
                    : null,
                IsDone = t.IsDone,
                DoneDate = t.DoneDate,
                IsActive = t.IsActive,
                IsCreatedByCurrentUser = currentUserId.HasValue && t.Task.CreatedBy == currentUserId,
            })
            .ToListAsync();

        return View(tasks);
    }

    public async Task<IActionResult> Details(int id)
    {
        var currentUserId = User.GetLegacyUserId();
        var task = await _context.AssignTasks
            .Where(t => t.Id == id && t.IsActive && t.Task != null && t.Task.IsActive)
            .Include(t => t.AssignedToNavigation)
            .Include(t => t.Task)
                .ThenInclude(t => t!.CreatedByNavigation)
            .Include(t => t.TaskHeadlineNavigation)
            .Select(t => new StaffTaskListItemViewModel
            {
                Id = t.Id,
                TaskId = t.TaskId ?? 0,
                Description = t.Task!.Description ?? string.Empty,
                DueDate = t.Task.Duedate,
                AssignedToName = t.AssignedToNavigation != null
                    ? t.AssignedToNavigation.FirstName + " " + t.AssignedToNavigation.LastName
                    : null,
                Headline = t.TaskHeadlineNavigation != null ? t.TaskHeadlineNavigation.Headline : null,
                CreatedByName = t.Task.CreatedByNavigation != null
                    ? t.Task.CreatedByNavigation.FirstName + " " + t.Task.CreatedByNavigation.LastName
                    : null,
                IsDone = t.IsDone,
                DoneDate = t.DoneDate,
                IsActive = t.IsActive,
                IsCreatedByCurrentUser = currentUserId.HasValue && t.Task.CreatedBy == currentUserId,
            })
            .FirstOrDefaultAsync();

        return task is null ? NotFound() : View(task);
    }

    public async Task<IActionResult> Board(int? headlineId)
    {
        var userId = User.GetLegacyUserId();
        if (userId is null)
        {
            return Forbid();
        }

        var headlines = await _context.TaskHeadlines
            .Where(headline => headline.UserId == userId && headline.IsActive == true)
            .OrderByDescending(headline => headline.Headline == "Assigned To")
            .ThenByDescending(headline => headline.Headline == "Assigned")
            .ThenBy(headline => headline.Headline)
            .Select(headline => new StaffTaskBoardHeadlineViewModel
            {
                Id = headline.Id,
                Headline = headline.Headline ?? string.Empty,
                OpenTaskCount = headline.AssignTasks.Count(task =>
                    task.AssignedTo == userId
                    && task.IsActive
                    && task.Task != null
                    && task.Task.IsActive
                    && !task.IsDone),
                UnreadTaskCount = headline.AssignTasks.Count(task =>
                    task.AssignedTo == userId
                    && task.IsActive
                    && task.Task != null
                    && task.Task.IsActive
                    && !task.IsDone
                    && !task.IsRead),
            })
            .ToListAsync();

        var selectedHeadline = headlineId.HasValue
            ? headlines.FirstOrDefault(headline => headline.Id == headlineId.Value)
            : headlines.FirstOrDefault(headline => headline.OpenTaskCount > 0) ?? headlines.FirstOrDefault();

        if (headlineId.HasValue && selectedHeadline is null)
        {
            return NotFound();
        }

        var tasks = new List<StaffTaskBoardTaskViewModel>();
        if (selectedHeadline is not null)
        {
            var assignedTasks = await _context.AssignTasks
                .Where(task =>
                    task.TaskHeadline == selectedHeadline.Id
                    && task.AssignedTo == userId
                    && task.IsActive
                    && task.Task != null
                    && task.Task.IsActive
                    && !task.IsDone)
                .Include(task => task.Task)
                    .ThenInclude(task => task!.CreatedByNavigation)
                .OrderBy(task => task.Task!.Duedate == null)
                .ThenBy(task => task.Task!.Duedate)
                .ThenByDescending(task => task.Id)
                .ToListAsync();

            if (string.Equals(selectedHeadline.Headline, "Assigned", StringComparison.OrdinalIgnoreCase))
            {
                var markedAny = false;
                foreach (var task in assignedTasks.Where(task => !task.IsRead))
                {
                    task.IsRead = true;
                    task.UpdatedOn = DateTime.UtcNow;
                    task.UpdatedBy = userId;
                    markedAny = true;
                }

                if (markedAny)
                {
                    await _context.SaveChangesAsync();
                    selectedHeadline.UnreadTaskCount = 0;
                }
            }

            tasks = assignedTasks
                .Select(task => new StaffTaskBoardTaskViewModel
                {
                    Id = task.Id,
                    TaskId = task.TaskId ?? 0,
                    Description = task.Task!.Description ?? string.Empty,
                    DueDate = task.Task.Duedate,
                    CreatedByName = task.Task.CreatedByNavigation != null
                        ? task.Task.CreatedByNavigation.FirstName + " " + task.Task.CreatedByNavigation.LastName
                        : null,
                    IsDone = task.IsDone,
                    IsRead = task.IsRead,
                    IsCreatedByCurrentUser = task.Task.CreatedBy == userId,
                })
                .ToList();
        }

        return View(new StaffTaskBoardViewModel
        {
            Headlines = headlines,
            SelectedHeadline = selectedHeadline,
            Tasks = tasks,
        });
    }

    public async Task<IActionResult> Create()
    {
        var model = new StaffTaskFormViewModel();
        await PopulateOptionsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> EditGroup(int taskId, string? returnUrl = null)
    {
        var userId = User.GetLegacyUserId();
        if (userId is null)
        {
            return Forbid();
        }

        var task = await _context.Tasks
            .Include(task => task.AssignTasks.Where(assignment => assignment.IsActive))
            .AsNoTracking()
            .FirstOrDefaultAsync(task => task.Id == taskId && task.CreatedBy == userId && task.IsActive);

        if (task is null)
        {
            return NotFound();
        }

        var model = new StaffTaskFormViewModel
        {
            TaskId = task.Id,
            Description = task.Description ?? string.Empty,
            DueDate = task.Duedate,
            AssignedToIds = task.AssignTasks
                .Where(assignment => assignment.AssignedTo.HasValue)
                .Select(assignment => assignment.AssignedTo!.Value)
                .Distinct()
                .ToList(),
            IsActive = task.IsActive,
            ReturnUrl = returnUrl,
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGroup(int taskId, StaffTaskFormViewModel model)
    {
        var userId = User.GetLegacyUserId();
        if (userId is null)
        {
            return Forbid();
        }

        if (taskId != model.TaskId)
        {
            return NotFound();
        }

        var assignedUserIds = GetAssignedUserIds(model);
        if (assignedUserIds.Count == 0)
        {
            ModelState.AddModelError(nameof(model.AssignedToIds), "Choose at least one assignee.");
        }

        await ValidateAssignedUsersAsync(assignedUserIds, nameof(model.AssignedToIds));

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var task = await _context.Tasks
            .Include(task => task.AssignTasks)
            .FirstOrDefaultAsync(task => task.Id == taskId && task.CreatedBy == userId && task.IsActive);

        if (task is null)
        {
            return NotFound();
        }

        task.Description = model.Description;
        task.Duedate = model.DueDate;
        task.IsActive = true;

        var activeAssignments = task.AssignTasks.Where(assignment => assignment.IsActive).ToList();
        foreach (var assignment in activeAssignments.Where(assignment => !assignment.AssignedTo.HasValue || !assignedUserIds.Contains(assignment.AssignedTo.Value)))
        {
            assignment.IsActive = false;
            assignment.UpdatedOn = DateTime.UtcNow;
            assignment.UpdatedBy = userId;
        }

        var existingActiveAssignedUserIds = activeAssignments
            .Where(assignment => assignment.AssignedTo.HasValue && assignedUserIds.Contains(assignment.AssignedTo.Value))
            .Select(assignment => assignment.AssignedTo!.Value)
            .ToHashSet();

        foreach (var assignedUserId in assignedUserIds.Where(assignedUserId => !existingActiveAssignedUserIds.Contains(assignedUserId)))
        {
            var inactiveAssignment = task.AssignTasks
                .Where(assignment => !assignment.IsActive && assignment.AssignedTo == assignedUserId)
                .OrderByDescending(assignment => assignment.Id)
                .FirstOrDefault();

            if (inactiveAssignment is not null)
            {
                inactiveAssignment.IsActive = true;
                inactiveAssignment.IsDone = false;
                inactiveAssignment.DoneDate = null;
                inactiveAssignment.IsRead = assignedUserId == userId;
                inactiveAssignment.UpdatedOn = DateTime.UtcNow;
                inactiveAssignment.UpdatedBy = userId;
                inactiveAssignment.TaskHeadline = await ResolveHeadlineAsync(assignedUserId, inactiveAssignment.TaskHeadline);
                continue;
            }

            task.AssignTasks.Add(new AssignTask
            {
                AssignedTo = assignedUserId,
                TaskHeadline = await ResolveHeadlineAsync(assignedUserId, null),
                IsActive = true,
                IsRead = assignedUserId == userId,
            });
        }

        await _context.SaveChangesAsync();

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffTaskFormViewModel model)
    {
        var assignedUserIds = GetAssignedUserIds(model);
        if (assignedUserIds.Count == 0)
        {
            ModelState.AddModelError(nameof(model.AssignedToIds), "Choose at least one assignee.");
        }

        await ValidateAssignedUsersAsync(assignedUserIds, nameof(model.AssignedToIds));

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var task = new TaskItem
        {
            Description = model.Description,
            Duedate = model.DueDate,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };

        foreach (var assignedUserId in assignedUserIds)
        {
            task.AssignTasks.Add(new AssignTask
            {
                AssignedTo = assignedUserId,
                TaskHeadline = await ResolveHeadlineAsync(assignedUserId, model.TaskHeadline),
                IsActive = true,
                IsRead = assignedUserId == User.GetLegacyUserId(),
            });
        }

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Headlines()
    {
        var userId = User.GetLegacyUserId();
        if (userId is null)
        {
            return Forbid();
        }

        var headlines = await _context.TaskHeadlines
            .Where(headline => headline.UserId == userId && headline.IsActive == true && headline.Headline != "Assigned")
            .OrderByDescending(headline => headline.Id)
            .Select(headline => new StaffTaskHeadlineListItemViewModel
            {
                Id = headline.Id,
                Headline = headline.Headline ?? string.Empty,
                OpenTaskCount = headline.AssignTasks.Count(task =>
                    task.IsActive
                    && task.Task != null
                    && task.Task.IsActive
                    && !task.IsDone
                    && task.AssignedTo == userId),
            })
            .ToListAsync();

        return View(headlines);
    }

    public IActionResult CreateHeadline()
    {
        return User.GetLegacyUserId() is null
            ? Forbid()
            : View(new StaffTaskHeadlineFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateHeadline(StaffTaskHeadlineFormViewModel model)
    {
        var userId = User.GetLegacyUserId();
        if (userId is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (IsReservedAssignedHeadline(model.Headline))
        {
            ModelState.AddModelError(nameof(model.Headline), "Assigned is a reserved task headline.");
            return View(model);
        }

        var duplicate = await _context.TaskHeadlines.AnyAsync(headline =>
            headline.UserId == userId
            && headline.IsActive == true
            && headline.Headline == model.Headline);
        if (duplicate)
        {
            ModelState.AddModelError(nameof(model.Headline), "You already have a task headline with this name.");
            return View(model);
        }

        _context.TaskHeadlines.Add(new TaskHeadline
        {
            UserId = userId,
            Headline = model.Headline,
            IsActive = true,
            CreateOn = DateTime.UtcNow,
            CreatedBy = userId,
        });
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Headlines));
    }

    public async Task<IActionResult> EditHeadline(int id)
    {
        var userId = User.GetLegacyUserId();
        if (userId is null)
        {
            return Forbid();
        }

        var headline = await _context.TaskHeadlines
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId && h.IsActive == true);

        if (headline is null)
        {
            return NotFound();
        }

        return View(new StaffTaskHeadlineFormViewModel
        {
            Id = headline.Id,
            Headline = headline.Headline ?? string.Empty,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditHeadline(int id, StaffTaskHeadlineFormViewModel model)
    {
        var userId = User.GetLegacyUserId();
        if (userId is null)
        {
            return Forbid();
        }

        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (IsReservedAssignedHeadline(model.Headline))
        {
            ModelState.AddModelError(nameof(model.Headline), "Assigned is a reserved task headline.");
            return View(model);
        }

        var headline = await _context.TaskHeadlines
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId && h.IsActive == true);
        if (headline is null)
        {
            return NotFound();
        }

        var duplicate = await _context.TaskHeadlines.AnyAsync(existing =>
            existing.Id != id
            && existing.UserId == userId
            && existing.IsActive == true
            && existing.Headline == model.Headline);
        if (duplicate)
        {
            ModelState.AddModelError(nameof(model.Headline), "You already have a task headline with this name.");
            return View(model);
        }

        headline.Headline = model.Headline;
        headline.UpdatedOn = DateTime.UtcNow;
        headline.UpdatedBy = userId;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Headlines));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateHeadline(int id)
    {
        var userId = User.GetLegacyUserId();
        if (userId is null)
        {
            return Forbid();
        }

        var headline = await _context.TaskHeadlines
            .FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId && h.IsActive == true);
        if (headline is null)
        {
            return NotFound();
        }

        var hasOpenTasks = await _context.AssignTasks.AnyAsync(task =>
            task.TaskHeadline == id
            && task.AssignedTo == userId
            && task.IsActive
            && task.Task != null
            && task.Task.IsActive
            && !task.IsDone);
        if (hasOpenTasks)
        {
            TempData["Error"] = "Complete or move open tasks before deleting this headline.";
            return RedirectToAction(nameof(Headlines));
        }

        headline.IsActive = false;
        headline.UpdatedOn = DateTime.UtcNow;
        headline.UpdatedBy = userId;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Headlines));
    }

    private static bool IsReservedAssignedHeadline(string? headline)
    {
        return string.Equals(headline?.Trim(), "Assigned", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IActionResult> Edit(int id, string? returnUrl = null)
    {
        var assignTask = await _context.AssignTasks
            .Include(t => t.Task)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (assignTask?.Task is null)
        {
            return NotFound();
        }

        var model = new StaffTaskFormViewModel
        {
            Id = assignTask.Id,
            TaskId = assignTask.TaskId,
            Description = assignTask.Task.Description ?? string.Empty,
            DueDate = assignTask.Task.Duedate,
            AssignedTo = assignTask.AssignedTo,
            AssignedToIds = assignTask.AssignedTo.HasValue ? [assignTask.AssignedTo.Value] : [],
            TaskHeadline = assignTask.TaskHeadline,
            IsActive = assignTask.IsActive && assignTask.Task.IsActive,
            ReturnUrl = returnUrl,
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StaffTaskFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!model.AssignedTo.HasValue && model.AssignedToIds.Count == 1)
        {
            model.AssignedTo = model.AssignedToIds[0];
        }

        if (!model.AssignedTo.HasValue)
        {
            ModelState.AddModelError(nameof(model.AssignedTo), "Choose an assignee.");
        }

        if (model.AssignedTo.HasValue)
        {
            await ValidateAssignedUsersAsync([model.AssignedTo.Value], nameof(model.AssignedTo));
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var assignTask = await _context.AssignTasks
            .Include(t => t.Task)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (assignTask?.Task is null)
        {
            return NotFound();
        }

        assignTask.Task.Description = model.Description;
        assignTask.Task.Duedate = model.DueDate;
        assignTask.AssignedTo = model.AssignedTo;
        assignTask.TaskHeadline = await ResolveHeadlineAsync(model.AssignedTo!.Value, model.TaskHeadline);
        assignTask.IsActive = model.IsActive;
        assignTask.UpdatedOn = DateTime.UtcNow;
        assignTask.UpdatedBy = User.GetLegacyUserId();

        await _context.SaveChangesAsync();

        return RedirectToLocalOrIndex(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDone(int id, bool done, string? returnUrl = null)
    {
        var assignTask = await _context.AssignTasks.FindAsync(id);
        if (assignTask is null)
        {
            return NotFound();
        }

        assignTask.IsDone = done;
        assignTask.DoneDate = done ? DateTime.UtcNow : null;
        assignTask.UpdatedOn = DateTime.UtcNow;
        assignTask.UpdatedBy = User.GetLegacyUserId();
        await _context.SaveChangesAsync();

        return RedirectToLocalOrIndex(returnUrl, new { includeCompleted = done });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id, string? returnUrl = null)
    {
        var assignTask = await _context.AssignTasks
            .Include(t => t.Task)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (assignTask is null)
        {
            return NotFound();
        }

        assignTask.IsActive = false;
        assignTask.UpdatedOn = DateTime.UtcNow;
        assignTask.UpdatedBy = User.GetLegacyUserId();

        if (assignTask.TaskId.HasValue
            && !await _context.AssignTasks.AnyAsync(t => t.TaskId == assignTask.TaskId && t.Id != id && t.IsActive))
        {
            assignTask.Task!.IsActive = false;
        }

        await _context.SaveChangesAsync();

        return RedirectToLocalOrIndex(returnUrl);
    }

    private IActionResult RedirectToLocalOrIndex(string? returnUrl, object? routeValues = null)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index), routeValues);
    }

    private async Task<int?> ResolveHeadlineAsync(int userId, int? selectedHeadlineId)
    {
        if (selectedHeadlineId.HasValue
            && await _context.TaskHeadlines.AnyAsync(h => h.Id == selectedHeadlineId && h.UserId == userId && h.IsActive == true))
        {
            return selectedHeadlineId;
        }

        var existingHeadlineId = await _context.TaskHeadlines
            .Where(h => h.UserId == userId && h.IsActive == true && h.Headline == "Assigned")
            .Select(h => (int?)h.Id)
            .FirstOrDefaultAsync();

        if (existingHeadlineId.HasValue)
        {
            return existingHeadlineId;
        }

        var headline = new TaskHeadline
        {
            UserId = userId,
            Headline = "Assigned",
            IsActive = true,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };
        _context.TaskHeadlines.Add(headline);
        await _context.SaveChangesAsync();
        return headline.Id;
    }

    private async Task PopulateOptionsAsync(StaffTaskFormViewModel model)
    {
        model.UserOptions = await _context.Users
            .Where(u => u.IsActive == true)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.FirstName + " " + u.LastName,
            })
            .ToListAsync();

        model.HeadlineOptions = await _context.TaskHeadlines
            .Where(h => h.IsActive == true && (!model.AssignedTo.HasValue || h.UserId == model.AssignedTo))
            .OrderBy(h => h.Headline)
            .Select(h => new SelectListItem { Value = h.Id.ToString(), Text = h.Headline })
            .ToListAsync();
    }

    private static List<int> GetAssignedUserIds(StaffTaskFormViewModel model)
    {
        var assignedUserIds = model.AssignedToIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (assignedUserIds.Count == 0 && model.AssignedTo.HasValue)
        {
            assignedUserIds.Add(model.AssignedTo.Value);
        }

        return assignedUserIds;
    }

    private async Task ValidateAssignedUsersAsync(IReadOnlyCollection<int> assignedUserIds, string modelKey)
    {
        if (assignedUserIds.Count == 0)
        {
            return;
        }

        var activeUserIds = await _context.Users
            .Where(user => user.IsActive == true && assignedUserIds.Contains(user.Id))
            .Select(user => user.Id)
            .ToListAsync();

        if (activeUserIds.Count != assignedUserIds.Count)
        {
            ModelState.AddModelError(modelKey, "Choose active assignees.");
        }
    }
}
