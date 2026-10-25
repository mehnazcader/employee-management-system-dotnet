
import { useEffect, useState } from 'react'
import './App.css'

const API_URL = '/api/employees'

const emptyEmployee = {
  fullName: '',
  email: '',
  department: '',
  jobTitle: '',
  isActive: true,
}

function App() {
  const [employees, setEmployees] = useState([])
  const [form, setForm] = useState(emptyEmployee)
  const [editingId, setEditingId] = useState(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

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
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

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

  function handleChange(event) {
    const { name, value, type, checked } = event.target

    setForm((current) => ({
      ...current,
      [name]: type === 'checkbox' ? checked : value,
    }))
  }

  function resetForm() {
    setForm({ ...emptyEmployee })
    setEditingId(null)
  }

  function editEmployee(employee) {
    setEditingId(employee.id)

    setForm({
      fullName: employee.fullName,
      email: employee.email,
      department: employee.department,
      jobTitle: employee.jobTitle,
      isActive: employee.isActive,
    })

    setError('')
    setMessage('')

    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  async function handleSubmit(event) {
    event.preventDefault()

    setSaving(true)
    setError('')
    setMessage('')

    const isEditing = editingId !== null
    const url = isEditing ? `${API_URL}/${editingId}` : API_URL

    try {
      const response = await fetch(url, {
        method: isEditing ? 'PUT' : 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(form),
      })

      if (!response.ok) {
        if (response.status === 409) {
          throw new Error('An employee with this email already exists.')
        }

        if (response.status === 400) {
          throw new Error('Please check the employee information.')
        }

        if (response.status === 404) {
          throw new Error('This employee no longer exists.')
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
      setError(err.message)
    } finally {
      setSaving(false)
    }
  }

  async function deleteEmployee(employee) {
    const confirmed = window.confirm(
      `Are you sure you want to delete ${employee.fullName}?`
    )

    if (!confirmed) {
      return
    }

    setError('')
    setMessage('')

    try {
      const response = await fetch(`${API_URL}/${employee.id}`, {
        method: 'DELETE',
      })

      if (!response.ok) {
        throw new Error('Could not delete employee.')
      }

      if (editingId === employee.id) {
        resetForm()
      }

      await loadEmployees()
      setMessage('Employee deleted successfully.')
    } catch (err) {
      setError(err.message)
    }
  }

  return (
    <main className="page">
      <header className="page-header">
        <div>
          <p className="eyebrow">EMPLOYEE DIRECTORY</p>

          <h1>Employee Management System</h1>

          <p className="subtitle">
            Create, view, update and delete employee records.
          </p>
        </div>

        <span className="count">
          {employees.length} employees
        </span>
      </header>

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

      <section className="panel">
        <h2>
          {editingId !== null ? 'Edit Employee' : 'Add Employee'}
        </h2>

        <form onSubmit={handleSubmit} className="employee-form">
          <div className="field">
            <label htmlFor="fullName">Full Name</label>

            <input
              id="fullName"
              name="fullName"
              value={form.fullName}
              onChange={handleChange}
              required
            />
          </div>

          <div className="field">
            <label htmlFor="email">Email</label>

            <input
              id="email"
              name="email"
              type="email"
              value={form.email}
              onChange={handleChange}
              required
            />
          </div>

          <div className="field">
            <label htmlFor="department">Department</label>

            <input
              id="department"
              name="department"
              value={form.department}
              onChange={handleChange}
              required
            />
          </div>

          <div className="field">
            <label htmlFor="jobTitle">Job Title</label>

            <input
              id="jobTitle"
              name="jobTitle"
              value={form.jobTitle}
              onChange={handleChange}
              required
            />
          </div>

          <label className="checkbox-field">
            <input
              name="isActive"
              type="checkbox"
              checked={form.isActive}
              onChange={handleChange}
            />

            Active employee
          </label>

          <div className="form-actions">
            <button type="submit" disabled={saving}>
              {saving
                ? 'Saving...'
                : editingId !== null
                  ? 'Save Changes'
                  : 'Add Employee'}
            </button>

            {editingId !== null && (
              <button
                type="button"
                className="secondary"
                onClick={resetForm}
              >
                Cancel Editing
              </button>
            )}
          </div>
        </form>
      </section>

      <section className="panel">
        <div className="section-header">
          <h2>Employees</h2>

          <button
            type="button"
            className="secondary"
            onClick={loadEmployees}
          >
            Refresh
          </button>
        </div>

        {loading ? (
          <p>Loading employees...</p>
        ) : employees.length === 0 ? (
          <p>No employees found. Add your first employee above.</p>
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
                {employees.map((employee) => (
                  <tr key={employee.id}>
                    <td>{employee.id}</td>
                    <td>{employee.fullName}</td>
                    <td>{employee.email}</td>
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
                        {employee.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>

                    <td>
                      <div className="row-actions">
                        <button
                          type="button"
                          className="secondary"
                          onClick={() => editEmployee(employee)}
                        >
                          Edit
                        </button>

                        <button
                          type="button"
                          className="danger"
                          onClick={() => deleteEmployee(employee)}
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
    </main>
  )
}

export default App
