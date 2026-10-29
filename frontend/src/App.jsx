
import { useEffect, useRef, useState } from 'react'
import './App.css'

// API used by the Employee Management System.
const API_URL = '/api/employees'

// Departments and their permitted job titles.
const departmentJobs = {
  'Information Technology': [
    'Software Developer',
    'Software Engineer',
    'IT Support Specialist',
    'System Administrator',
    'QA Engineer',
  ],
  'Human Resources': [
    'HR Officer',
    'HR Manager',
    'Recruitment Specialist',
    'HR Assistant',
  ],
  Finance: [
    'Accountant',
    'Financial Analyst',
    'Finance Officer',
    'Finance Manager',
  ],
  Marketing: [
    'Marketing Executive',
    'Digital Marketing Specialist',
    'Marketing Manager',
    'Content Specialist',
  ],
  Operations: [
    'Operations Coordinator',
    'Operations Manager',
    'Operations Analyst',
    'Administrative Officer',
  ],
  Sales: [
    'Sales Executive',
    'Sales Representative',
    'Sales Manager',
    'Account Manager',
  ],
}

// The employee ID is generated automatically by SQL Server.
const emptyEmployee = {
  fullName: '',
  email: '',
  department: '',
  jobTitle: '',
  isActive: true,
}

// Full-name validation.
// Names must begin with a letter.
// Spaces, apostrophes, periods and hyphens are accepted
// when they separate name parts.
// Unicode letters are supported, including accented letters.
const namePattern =
  /^\p{L}[\p{L}\p{M}]*(?:[ .'-]\p{L}[\p{L}\p{M}]*)*$/u

function App() {
  // Employees from SQL Server.
  const [employees, setEmployees] = useState([])

  // Form values and employee being edited.
  const [form, setForm] = useState({ ...emptyEmployee })
  const [editingId, setEditingId] = useState(null)

  // Loading states.
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [importing, setImporting] = useState(false)

  // Feedback messages.
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  // Employee search.
  const [searchTerm, setSearchTerm] = useState('')

  // Input validation message.
  const [nameError, setNameError] = useState('')

  // References.
  const formRef = useRef(null)
  const excelInputRef = useRef(null)

  // ==================================================
  // VALIDATE EMPLOYEE NAME
  // ==================================================
  function validateName(name) {
    const trimmedName = name.trim()

    if (!trimmedName) {
      return 'Full name is required.'
    }

    if (!namePattern.test(trimmedName)) {
      return (
        'Enter a valid name using letters. ' +
        'Spaces, hyphens and apostrophes are allowed.'
      )
    }

    return ''
  }

  // ==================================================
  // SEARCH EMPLOYEES
  // Filter the displayed list without modifying SQL Server.
  // ==================================================
  const filteredEmployees = employees.filter((employee) =>
    employee.fullName
      .toLowerCase()
      .includes(searchTerm.trim().toLowerCase())
  )

  // ==================================================
  // DOWNLOAD EXCEL
  // GET /api/employees/export
  // Downloads all saved employees.
  // ==================================================
  async function exportEmployees() {
    setError('')
    setMessage('')

    try {
      const response = await fetch(`${API_URL}/export`)

      if (!response.ok) {
        throw new Error('Could not download the Excel file.')
      }

      const blob = await response.blob()
      const url = window.URL.createObjectURL(blob)

      // Create a temporary link for the Excel download.
      const link = document.createElement('a')
      link.href = url
      link.download = 'Employees.xlsx'

      document.body.appendChild(link)
      link.click()

      // Clean up temporary browser resources.
      document.body.removeChild(link)
      window.URL.revokeObjectURL(url)

      setMessage('Employee Excel file downloaded successfully.')
    } catch (err) {
      setError(err.message || 'Excel download failed.')
    }
  }

  // ==================================================
  // UPLOAD EXCEL
  // POST /api/employees/import
  // The backend adds only new employees.
  // ==================================================
  async function importEmployees(event) {
    const file = event.target.files?.[0]

    if (!file) return

    setError('')
    setMessage('')

    if (!file.name.toLowerCase().endsWith('.xlsx')) {
      setError('Please select an Excel file ending in .xlsx.')
      event.target.value = ''
      return
    }

    setImporting(true)

    try {
      const formData = new FormData()
      formData.append('file', file)

      const response = await fetch(`${API_URL}/import`, {
        method: 'POST',
        body: formData,
      })

      const responseText = await response.text()
      let result

      try {
        result = JSON.parse(responseText)
      } catch {
        result = responseText
      }

      if (!response.ok) {
        const errorMessage =
          typeof result === 'string'
            ? result
            : result?.message ||
              result?.title ||
              'Could not import employees.'

        throw new Error(errorMessage)
      }

      // Refresh the directory after importing.
      await loadEmployees()

      setMessage(
        result?.message || 'Employees imported successfully.'
      )
    } catch (err) {
      setError(err.message || 'Could not upload the Excel file.')
    } finally {
      setImporting(false)
      event.target.value = ''
    }
  }

  // ==================================================
  // GET EMPLOYEES
  // GET /api/employees
  // ==================================================
  async function loadEmployees() {
    setLoading(true)
    setError('')

    try {
      const response = await fetch(API_URL)

      if (!response.ok) {
        throw new Error('Could not load employees.')
      }

      const data = await response.json()
      setEmployees(data)
    } catch (err) {
      setError(err.message || 'Could not load employees.')
    } finally {
      setLoading(false)
    }
  }

  // Retrieve employees when the page first loads.
  useEffect(() => {
    let cancelled = false

    fetch(API_URL)
      .then((response) => {
        if (!response.ok) {
          throw new Error('Could not load employees.')
        }

        return response.json()
      })
      .then((data) => {
        if (!cancelled) {
          setEmployees(data)
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err.message)
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false)
        }
      })

    return () => {
      cancelled = true
    }
  }, [])

  // ==================================================
  // HANDLE FORM CHANGES
  // ==================================================
  function handleChange(event) {
    const { name, value, type, checked } = event.target

    // Validate the name while the user types.
    if (name === 'fullName') {
      setNameError(value.trim() ? validateName(value) : '')
    }

    setForm((current) => {
      // Changing department resets the previous job title.
      if (name === 'department') {
        return {
          ...current,
          department: value,
          jobTitle: '',
        }
      }

      // Automatically convert emails to lowercase.
      if (name === 'email') {
        return {
          ...current,
          email: value.toLowerCase(),
        }
      }

      return {
        ...current,
        [name]: type === 'checkbox' ? checked : value,
      }
    })
  }

  // Reset the employee form.
  function resetForm() {
    setForm({ ...emptyEmployee })
    setEditingId(null)
    setNameError('')
  }

  // ==================================================
  // EDIT EMPLOYEE
  // ==================================================
  function editEmployee(employee) {
    setEditingId(employee.id)

    setForm({
      fullName: employee.fullName,
      email: employee.email.toLowerCase(),
      department: employee.department,
      jobTitle: employee.jobTitle,
      isActive: employee.isActive,
    })

    setNameError('')
    setError('')
    setMessage('')

    formRef.current?.scrollIntoView({
      behavior: 'smooth',
      block: 'nearest',
    })
  }

  // ==================================================
  // ADD OR UPDATE EMPLOYEE
  // POST /api/employees
  // PUT /api/employees/{id}
  // ==================================================
  async function handleSubmit(event) {
    event.preventDefault()

    setError('')
    setMessage('')

    // Validate the name before contacting the API.
    const validationError = validateName(form.fullName)

    if (validationError) {
      setNameError(validationError)
      setError(validationError)
      return
    }

    // Check that the job title belongs to the department.
    if (!departmentJobs[form.department]?.includes(form.jobTitle)) {
      setError(
        'Please select a valid job title for the selected department.'
      )
      return
    }

    setSaving(true)

    const isEditing = editingId !== null

    const url = isEditing
      ? `${API_URL}/${editingId}`
      : API_URL

    try {
      // Clean the values before sending them to the API.
      const employeeData = {
        ...form,
        fullName: form.fullName.trim().replace(/\s+/g, ' '),
        email: form.email.trim().toLowerCase(),
      }

      const response = await fetch(url, {
        method: isEditing ? 'PUT' : 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(employeeData),
      })

      if (!response.ok) {
        if (response.status === 409) {
          throw new Error(
            'An employee with this email already exists.'
          )
        }

        if (response.status === 400) {
          throw new Error(
            'Please check the employee information.'
          )
        }

        if (response.status === 404) {
          throw new Error(
            'This employee no longer exists.'
          )
        }

        throw new Error('Could not save employee.')
      }

      resetForm()
      await loadEmployees()

      setMessage(
        isEditing
          ? 'Employee updated successfully.'
          : 'Employee added successfully.'
      )
    } catch (err) {
      setError(err.message || 'Could not save employee.')
    } finally {
      setSaving(false)
    }
  }

  // ==================================================
  // DELETE EMPLOYEE
  // DELETE /api/employees/{id}
  // ==================================================
  async function deleteEmployee(employee) {
    const confirmed = window.confirm(
      `Are you sure you want to delete ${employee.fullName}?`
    )

    if (!confirmed) return

    setError('')
    setMessage('')

    try {
      const response = await fetch(
        `${API_URL}/${employee.id}`,
        {
          method: 'DELETE',
        }
      )

      if (!response.ok) {
        throw new Error('Could not delete employee.')
      }

      if (editingId === employee.id) {
        resetForm()
      }

      await loadEmployees()
      setMessage('Employee deleted successfully.')
    } catch (err) {
      setError(err.message || 'Could not delete employee.')
    }
  }

  // Generate initials for the employee avatar.
  function getInitials(fullName) {
    return fullName
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part.charAt(0).toUpperCase())
      .join('')
  }

  // Get available job titles for the selected department.
  const availableJobs = departmentJobs[form.department] || []

  // Preserve older department and job title values during editing.
  const existingDepartment =
    form.department && !departmentJobs[form.department]
      ? form.department
      : null

  const existingJobTitle =
    editingId !== null &&
    form.jobTitle &&
    !availableJobs.includes(form.jobTitle)
      ? form.jobTitle
      : null

  return (
    <div className="app-shell">

      {/* SIDEBAR */}
      <aside className="sidebar">
        <div className="brand">
          <div className="brand-icon">EM</div>

          <div className="brand-text">
            <strong>Employee</strong>
            <span>Management System</span>
          </div>
        </div>

        <p className="sidebar-label">WORKSPACE</p>

        <div className="sidebar-current">
          <span className="sidebar-symbol" aria-hidden="true">
            ▦
          </span>

          <span>Employees</span>
        </div>

        <div className="sidebar-footer">
          <div className="footer-symbol" aria-hidden="true">
            ✦
          </div>

          <strong>Employee Management</strong>

          <p>
            A simple solution for managing employee records.
          </p>
        </div>
      </aside>

      {/* MAIN CONTENT */}
      <main className="main-content">

        {/* Page heading */}
        <header className="top-header">
          <div>
            <p className="eyebrow">EMPLOYEE DIRECTORY</p>

            <h1>Employee Management System</h1>

            <p className="subtitle">
              Create, view, update and delete employee records.
            </p>
          </div>

          <div className="header-decoration" aria-hidden="true">
            ✦
          </div>
        </header>

        {/* Feedback messages */}
        {error && (
          <div className="alert error" role="alert">
            {error}
          </div>
        )}

        {message && (
          <div className="alert success" role="status">
            {message}
          </div>
        )}

        <div className="content-grid">

          {/* EMPLOYEE DIRECTORY */}
          <section className="panel directory-panel">

            <div className="section-header">
              <div>
                <p className="section-kicker">
                  EMPLOYEE RECORDS
                </p>

                <h2>Employee Directory</h2>

                <p className="section-description">
                  View and manage registered employees.
                </p>
              </div>

              {/* Total saved employees. */}
              <span className="count">
                {employees.length}{' '}
                {employees.length === 1
                  ? 'employee'
                  : 'employees'}
              </span>
            </div>

            {/* EMPLOYEE SEARCH */}
            <div className="field" style={{ marginBottom: '16px' }}>
              <label htmlFor="employeeSearch">
                Search Employees
              </label>

              <input
                id="employeeSearch"
                type="search"
                placeholder="Search employees by name..."
                value={searchTerm}
                onChange={(event) =>
                  setSearchTerm(event.target.value)
                }
              />

              {searchTerm.trim() && (
                <p className="section-description">
                  {filteredEmployees.length}{' '}
                  {filteredEmployees.length === 1
                    ? 'employee found'
                    : 'employees found'}
                </p>
              )}
            </div>

            {/* EXCEL BUTTONS */}
            <div className="directory-toolbar">

              {/* Hidden input for selecting Excel files. */}
              <input
                ref={excelInputRef}
                type="file"
                accept=".xlsx"
                onChange={importEmployees}
                aria-label="Choose an Excel file to upload"
                style={{ display: 'none' }}
              />

              <button
                type="button"
                className="primary-button"
                onClick={() =>
                  excelInputRef.current?.click()
                }
                disabled={loading || importing}
              >
                {importing ? 'Uploading...' : 'Upload Excel'}
              </button>

              <button
                type="button"
                className="secondary"
                onClick={exportEmployees}
                disabled={loading || importing}
              >
                Download Excel
              </button>
            </div>

            {/* EMPLOYEE TABLE */}
            {loading ? (
              <div className="empty-state">
                Loading employees...
              </div>
            ) : employees.length === 0 ? (
              <div className="empty-state">
                <div className="empty-symbol" aria-hidden="true">
                  ◎
                </div>

                <h3>No employees found</h3>

                <p>
                  Add your first employee using the form,
                  or upload an Excel file.
                </p>
              </div>
            ) : filteredEmployees.length === 0 ? (
              <div className="empty-state">
                <h3>No matching employees</h3>

                <p>
                  Try searching for a different name.
                </p>
              </div>
            ) : (
              <div className="table-wrapper">
                <table>
                  <thead>
                    <tr>
                      <th>ID</th>
                      <th>Full Name</th>
                      <th>Email</th>
                      <th>Department</th>
                      <th>Job Title</th>
                      <th>Status</th>
                      <th>Actions</th>
                    </tr>
                  </thead>

                  <tbody>
                    {filteredEmployees.map((employee) => (
                      <tr key={employee.id}>

                        <td className="id-cell">
                          #{employee.id}
                        </td>

                        <td>
                          <div className="employee-name-cell">
                            <span className="avatar" aria-hidden="true">
                              {getInitials(employee.fullName)}
                            </span>

                            <strong>
                              {employee.fullName}
                            </strong>
                          </div>
                        </td>

                        <td className="email-cell">
                          {employee.email}
                        </td>

                        <td>{employee.department}</td>

                        <td>{employee.jobTitle}</td>

                        <td>
                          <span
                            className={
                              employee.isActive
                                ? 'status active'
                                : 'status inactive'
                            }
                          >
                            {employee.isActive
                              ? 'Active'
                              : 'Inactive'}
                          </span>
                        </td>

                        <td>
                          <div className="row-actions">

                            <button
                              type="button"
                              className="secondary"
                              onClick={() =>
                                editEmployee(employee)
                              }
                            >
                              Edit
                            </button>

                            <button
                              type="button"
                              className="danger"
                              onClick={() =>
                                deleteEmployee(employee)
                              }
                            >
                              Delete
                            </button>

                          </div>
                        </td>

                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>

          {/* ADD / EDIT EMPLOYEE FORM */}
          <section
            className="panel form-panel"
            ref={formRef}
          >

            <div className="form-heading">
              <div className="form-heading-icon" aria-hidden="true">
                {editingId !== null ? '✎' : '+'}
              </div>

              <div>
                <h2>
                  {editingId !== null
                    ? 'Edit Employee'
                    : 'Add New Employee'}
                </h2>

                <p className="section-description">
                  {editingId !== null
                    ? 'Update the employee details below.'
                    : 'Fill in the employee details below.'}
                </p>
              </div>
            </div>

            <form
              className="employee-form"
              onSubmit={handleSubmit}
            >

              {/* FULL NAME */}
              <div className="field">
                <label htmlFor="fullName">
                  Full Name *
                </label>

                <input
                  id="fullName"
                  name="fullName"
                  type="text"
                  placeholder="Enter full name"
                  value={form.fullName}
                  onChange={handleChange}
                  aria-invalid={Boolean(nameError)}
                  required
                />

                {/* NEW: Display an inline name validation message. */}
                {nameError && (
                  <p
                    className="section-description"
                    style={{
                      color: '#c62828',
                      marginTop: '5px',
                    }}
                    role="alert"
                  >
                    {nameError}
                  </p>
                )}
              </div>

              {/* EMAIL */}
              <div className="field">
                <label htmlFor="email">
                  Email Address *
                </label>

                <input
                  id="email"
                  name="email"
                  type="email"
                  placeholder="Enter email address"
                  value={form.email}
                  onChange={handleChange}
                  required
                />
              </div>

              {/* DEPARTMENT */}
              <div className="field">
                <label htmlFor="department">
                  Department *
                </label>

                <select
                  id="department"
                  name="department"
                  value={form.department}
                  onChange={handleChange}
                  required
                >
                  <option value="" disabled hidden>
                    Select department
                  </option>

                  {Object.keys(departmentJobs).map(
                    (department) => (
                      <option
                        key={department}
                        value={department}
                      >
                        {department}
                      </option>
                    )
                  )}

                  {existingDepartment && (
                    <option
                      value={existingDepartment}
                      disabled
                    >
                      {existingDepartment} (existing value)
                    </option>
                  )}
                </select>
              </div>

              {/* JOB TITLE */}
              <div className="field">
                <label htmlFor="jobTitle">
                  Job Title *
                </label>

                <select
                  id="jobTitle"
                  name="jobTitle"
                  value={form.jobTitle}
                  onChange={handleChange}
                  disabled={!form.department}
                  required
                >
                  <option value="" disabled hidden>
                    {form.department
                      ? 'Select job title'
                      : 'Select a department first'}
                  </option>

                  {availableJobs.map((jobTitle) => (
                    <option
                      key={jobTitle}
                      value={jobTitle}
                    >
                      {jobTitle}
                    </option>
                  ))}

                  {existingJobTitle && (
                    <option
                      value={existingJobTitle}
                      disabled
                    >
                      {existingJobTitle} (existing value)
                    </option>
                  )}
                </select>
              </div>

              {/* STATUS */}
              <div className="field">
                <label className="status-label">
                  Status
                </label>

                <label className="checkbox-field">
                  <input
                    name="isActive"
                    type="checkbox"
                    checked={form.isActive}
                    onChange={handleChange}
                  />

                  <span>Active employee</span>
                </label>
              </div>

              {/* FORM ACTIONS */}
              <div className="form-actions">

                <button
                  type="submit"
                  className="primary-button"
                  disabled={saving}
                >
                  {saving
                    ? 'Saving...'
                    : editingId !== null
                      ? 'Save Changes'
                      : '+ Add Employee'}
                </button>

                {editingId !== null && (
                  <button
                    type="button"
                    className="secondary"
                    onClick={resetForm}
                    disabled={saving}
                  >
                    Cancel Editing
                  </button>
                )}
              </div>
            </form>
          </section>

        </div>
      </main>
    </div>
  )
}

export default App
