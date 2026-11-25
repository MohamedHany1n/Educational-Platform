# Test Cases: Teacher Courses and Lessons

Module owner: Mohamed Medhat
Covers: FR-06 (courses), FR-07 (publish), FR-08 (lessons, video link, reorder)

**Precondition:** the app is running, an admin account exists, and a teacher account was created by the admin
(Admin > Teachers). Log in as that teacher unless the test says otherwise.

## Courses (FR-06, FR-07)

| ID | Test | Steps | Expected result | Status |
|----|------|-------|-----------------|--------|
| TC-01 | Open the courses page | Click "My courses" | The page lists only this teacher's courses; an empty message shows if none | |
| TC-02 | Create a valid course | New course, fill title, description and price, save | Success message; the course appears in the list as a draft | |
| TC-03 | Title is required | Create a course with an empty title | Validation error, nothing is saved | |
| TC-04 | Invalid price | Enter a negative price | Validation error, nothing is saved | |
| TC-05 | Upload a valid thumbnail | Create a course with a JPG under 2 MB | Course is saved and the image shows in the edit page | |
| TC-06 | Reject an invalid thumbnail | Upload a `.txt` file or an image over 2 MB | Error message, course is not saved | |
| TC-07 | Edit a course | Change title and price, save | Success message; the list shows the new values | |
| TC-08 | Replace a thumbnail | Edit a course and upload a new image | New image shows; the old file is removed from `wwwroot/uploads/thumbnails` | |
| TC-09 | Publish a course | Click "Publish" in the list | Status changes to Published | |
| TC-10 | Unpublish a course | Click "Unpublish" | Status changes to Draft | |
| TC-11 | Delete a course with no relations | Delete a new empty course | Course is removed and its thumbnail file is deleted | |
| TC-12 | Delete a course with enrollments | Delete a course a student is enrolled in | Delete is refused with an error message; the course stays | |
| TC-13 | Access another teacher's course | Change the course id in the Edit URL to another teacher's course | 404 Not Found | |
| TC-14 | Role restriction | Log in as a Student and open `/TeacherCourses` | Access is denied | |
| TC-15 | Anonymous access | Log out and open `/TeacherCourses` | Redirected to the login page | |

## Lessons and videos (FR-08)

| ID | Test | Steps | Expected result | Status |
|----|------|-------|-----------------|--------|
| TC-16 | Open course lessons | Click "Lessons" on a course | The lessons list opens (empty message if none) | |
| TC-17 | Add a lesson | New lesson with title and `https://...` video link | Lesson is added; order is the next number | |
| TC-18 | Title and link are required | Submit with an empty title or empty link | Validation errors, nothing is saved | |
| TC-19 | Reject an invalid video link | Enter `abc` or `javascript:alert(1)` | Error message, lesson is not saved | |
| TC-20 | Edit a lesson | Change the title and link, save | Success message; the list shows the new values | |
| TC-21 | Move a lesson up | Click the up arrow on the second lesson | It becomes first; the order numbers are 1, 2, 3 | |
| TC-22 | Move a lesson down | Click the down arrow on the first lesson | It becomes second | |
| TC-23 | Edge buttons | Look at the first and last lessons | Up is disabled on the first, down is disabled on the last | |
| TC-24 | Delete a lesson | Delete a lesson nobody watched | Lesson is removed | |
| TC-25 | Delete a watched lesson | Delete a lesson with student progress | Delete is refused with an error message | |
| TC-26 | Access another teacher's lessons | Open `/TeacherLessons?courseId=` of another teacher's course | 404 Not Found | |
| TC-27 | Edit another teacher's lesson | Change the lesson id in the Edit URL | 404 Not Found | |

## Result summary

| Total | Passed | Failed | Date | Tester |
|-------|--------|--------|------|--------|
| 27 | | | | |
