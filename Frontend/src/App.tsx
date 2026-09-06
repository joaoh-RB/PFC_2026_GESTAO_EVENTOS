import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthProvider';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Login } from './pages/Login';
import { Register } from './pages/Register';
import { Dashboard } from './pages/Dashboard';
import { UserSettings } from './pages/UserSettings';
import { AuthenticatedLayout } from './components/AuthenticatedLayout';
import { CreateInstitution } from './pages/CreateInstitution';
import { CreateCourse } from './pages/CreateCourse';

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

                    <Route path="*" element={<Navigate to="/login" replace />} />

                </Routes>
            </AuthProvider>
        </BrowserRouter>
    );
}