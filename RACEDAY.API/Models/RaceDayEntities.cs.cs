
using System;
using System.Collections.Generic;

namespace RaceDay.Api.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = "Participant";
        public DateTime CreatedAt { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }

    public class Event
    {
        public int EventID { get; set; }
        public int OrganiserID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string EventType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public User? Organiser { get; set; }
        public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
        public Route? Route { get; set; }
    }

    public class Category
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryType { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();
    }

    public class EventCategory
    {
        public int EventCategoryID { get; set; }
        public int EventID { get; set; }
        public int CategoryID { get; set; }
        public decimal EntryFee { get; set; }
        public int MaxEntries { get; set; }
        public TimeSpan StartTime { get; set; }
        public bool IsActive { get; set; }

        public Event? Event { get; set; }
        public Category? Category { get; set; }
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }

    public class Enrolment
    {
        public int EnrolmentID { get; set; }
        public int UserID { get; set; }
        public int EventCategoryID { get; set; }
        public DateTime EnrolmentDate { get; set; }
        public string Status { get; set; } = "Pending";

        public User? User { get; set; }
        public EventCategory? EventCategory { get; set; }
        public Result? Result { get; set; }
    }

    public class Result
    {
        public int ResultID { get; set; }
        public int EnrolmentID { get; set; }
        public TimeSpan? ChipTime { get; set; }
        public TimeSpan? GunTime { get; set; }
        public int? PositionOverall { get; set; }
        public int? PositionCategory { get; set; }
        public bool IsCompleted { get; set; }
        public string? Notes { get; set; }
        public DateTime RecordedAt { get; set; }

        public Enrolment? Enrolment { get; set; }
    }

    public class Route
    {
        public int RouteID { get; set; }
        public int EventID { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal? ElevationGain { get; set; }
        public string? RouteDescription { get; set; }
        public string? MapUrl { get; set; }

        public Event? Event { get; set; }
    }
}
