import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthProvider';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Login } from './pages/Login';
import { Register } from './pages/Register';
import { Dashboard } from './pages/Dashboard';
import { UserSettings } from './pages/UserSettings';
import { AuthenticatedLayout } from './components/AuthenticatedLayout';
<<<<<<< HEAD
import { CreateInstitution } from './pages/CreateInstitution';
import { CreateCourse } from './pages/CreateCourse';

=======
import { Events } from './pages/Events';
>>>>>>> 7b0c935aabc5bad94f5fb9dba2e4cece0a6b6072
export default function App() {
    return (
        <BrowserRouter>
            <AuthProvider>
                <Routes>
                    {/* Rotas Públicas */}
                    <Route path="/login" element={<Login />} />
                    <Route path="/register" element={<Register />} />

                    {/* Rotas Privadas */}
                    <Route element={<ProtectedRoute />}>
                        <Route element={<AuthenticatedLayout />}>
                            <Route path="/dashboard" element={<Dashboard />} />
                            <Route path="/settings" element={<UserSettings />} />
                            <Route path="/institutions/new" element={<CreateInstitution />} />
                            <Route path="/courses/new" element={<CreateCourse />} />
                        </Route>
                    </Route>
                    <Route element={<ProtectedRoute allowedRoles={["Administrador", "Professor"]} />}>
                        <Route element={<AuthenticatedLayout/>}>
                            <Route path="/events" element={<Events />} />
                        </Route>
                    </Route>

                    <Route path="*" element={<Navigate to="/login" replace />} />

                </Routes>
            </AuthProvider>
        </BrowserRouter>
    );
}