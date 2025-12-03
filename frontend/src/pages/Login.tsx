import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { login } from "../lib/api";
import Input, { Label } from "../components/Input";
import Button from "../components/Button";
import { Card, CardBody, CardHeader } from "../components/Card";

export default function Login() {
  const [email, setEmail] = useState("admin@sunskog.local");
  const [password, setPassword] = useState("Admin123!");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await login(email, password);
      navigate("/timesheets", { replace: true });
    } catch (err: any) {
      setError(err?.message ?? "Přihlášení selhalo");
    } finally {
      setLoading(false);
    }
  }

  return (
    <Card>
      <CardHeader title="Přihlášení" subtitle="Zadejte přístupové údaje" />
      <CardBody>
        <form onSubmit={onSubmit} className="space-y-4">
          <div>
            <Label htmlFor="email">Email</Label>
            <Input id="email" type="email" value={email} onChange={e=>setEmail(e.target.value)} required />
          </div>
          <div>
            <Label htmlFor="pw">Heslo</Label>
            <Input id="pw" type="password" value={password} onChange={e=>setPassword(e.target.value)} required />
          </div>
          {error && <div className="text-sm text-red-600">{error}</div>}
          <div className="pt-2">
            <Button type="submit" loading={loading} className="w-full">Přihlásit</Button>
          </div>
        </form>
      </CardBody>
    </Card>
  );
}