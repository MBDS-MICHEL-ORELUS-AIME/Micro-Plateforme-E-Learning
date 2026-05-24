using eLearning_Project.Data;
using eLearning_Project.Models;
using eLearning_Project.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eLearning_Project.Controllers;

public class CoursesController(AppDbContext dbContext) : Controller
{
    private readonly AppDbContext _dbContext = dbContext;

    // Ce controleur gere encore le catalogue principal des cours. La gestion des modules et lecons viendra par-dessus.
    public async Task<IActionResult> Index()
    {
        var courses = await _dbContext.Courses
            .OrderByDescending(course => course.CreatedAt)
            .ToListAsync();

        return View(courses);
    }

    [Authorize(Roles = RoleNames.Admin)]
    public IActionResult Create()
    {
        return View(new Course());
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Course course)
    {
        if (!ModelState.IsValid)
        {
            return View(course);
        }

        _dbContext.Courses.Add(course);
        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var course = await _dbContext.Courses.FindAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        return View(course);
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Course course)
    {
        if (id != course.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(course);
        }

        var existingCourse = await _dbContext.Courses.FindAsync(id);
        if (existingCourse is null)
        {
            return NotFound();
        }

        existingCourse.Title = course.Title;
        existingCourse.Instructor = course.Instructor;
        existingCourse.Category = course.Category;
        existingCourse.DurationHours = course.DurationHours;
        existingCourse.Description = course.Description;

        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await _dbContext.Courses.FindAsync(id);
        if (course is null)
        {
            return NotFound();
        }

        return View(course);
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost, ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var course = await _dbContext.Courses.FindAsync(id);
        if (course is null)
        {
            return RedirectToAction(nameof(Index));
        }

        _dbContext.Courses.Remove(course);
        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}