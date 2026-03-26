# Product Requirements Document (PRD)

## Chronocode
**Task time tracking with charge code support**

## Document Owner
*James Hutchison*

---

## 1. Purpose
The goal of Chronocode is to provide a web-based tool that allows engineers to track multiple projects they are invovled in, and group set's of activities into tasks that allows them to add data like charge codes and activity durations to an activity so that they can easily update time cards, as well as generate activity reports.

## 2. Background & Context
Engineers that support multiple program, departments, and efforts can have a difficult time keeping up with the plethora of charge codes that they use for a given set of time during their day.  Chronocode intents to provide a single, web based way for engineers to enter their Projects, tasks, and activities in a way that allows them to quickly provide reporting guidance to their managers, and easily review what charge codes and durations where applied against those codes.

## 3. Goals & Objectives
- Provide a way to manage Projects (top level of work organization)
- Provide a way to manage Tasks (middle level of work organization)
- Provide a way to manage Activities (lowest level of work organization)
- Manage multiple charge codes at the project level, including validity date range periods 
- Allow the ability to allocate one charge code per Activity, or spread the time of an activity across some, or all charge codes in the Project evenly.
- Provide a daily, weekly, and custom date range activity report, describing all activities performed by a given engineer for that day, to include Activity description, associated Task, and any charge codes and associated activity duration per charge code.  


## 4. Stakeholders
| Name           | Role                | Responsibilities        |
|----------------|---------------------|---------------------------------------------------------|
| James H.       | Business Owner      | main reviewer of application      |


## 5. User Personas

### Persona 1: Engineer
- **Description:** Primary user of the app.  
- **Needs:** Ability to quickly add task and activity data, manage charge codes, and generate activity reports.
- **Pain Points:** Keeping track of all of the different projects they support; keeping their timecard up to date given the many charge codes they have to keep up with.

### Persona 2: Manager
- **Description:** Functional manager or task lead who manages engineers.
- **Needs:** A way to keep track of what their assigned engineers are working on; ability to issue charging guidance to their engineers in a auditable way.
- **Pain Points:** Verifying proper charging guidance by their engineers.  Enforcing charge code validity timeframes.


## 6. Features & Requirements

### 6.1. Core Features
- Ability to support multiple projects.  
- Assiciate multiple tasks per project
- Manage multiple activities per project.
- Track time in hours for activities
- Track one or more charge codes per activity
- Track Charge codes at the project level.
- Track engineers at the project level.
- Track Mamangers at the projec level.

### 6.2. Nice-to-Have Features
- Apply a group of charge codes per a single activity, and evenly distribute the activity hour allocation evenly across all selected charge codes.
- Reminders of when charge codes are about to expire

### 6.3. Out of Scope
- None

## 7. User Stories
- As an engineer, I want to be able to add new Projects so that I can organize my work taskings in a logical manner.
- As an engineer, I want to be able to add Tasks to my Projects so that I can organize my charging guidance.
- As an engineer, I want to be ableto add Activiites to my Tasks so that I can track my individual work activities throughout the day and track how much time I spent per activity.
- As an engineer, I want the ability to associate one, or more charge codes to a given activity, based on the list of charge codes associated with a given Task 
- As an engineer, I want to be able to run a daily report that shows all of my logged activitiies for the day, their hours and associated charge codes, which Task they are associated with, and activity description so that I can properly enter my time into an external time tracking system.
- As an engineer, I want to be able to run actifity reports so that I can provide an easy way to reporting what I worked on in a given week to my manager.


## 8. User Flows / Wireframes
- n/a

## 11. Constraints
- Application must be a Blazer web app
- Application must use SQlite 3 as the database backend


---
*Last updated: 19 July 2025*
