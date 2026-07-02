import { useState } from 'react';
import { View, Text, TextInput, Pressable, StyleSheet } from 'react-native';
import { router } from 'expo-router';
import { useAuth } from '@/hooks/useAuth';

export default function LoginScreen() {
  const { login, isAuthenticating } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit() {
    setFormError(null);

    if (!email || !password) {
      setFormError('Email y contraseña son requeridos.');
      return;
    }

    const success = await login(email, password);

    if (success) {
      router.replace('/(app)');
    } else {
      setFormError('Credenciales inválidas.');
    }
  }

  return (
    <View style={styles.container}>
      <Text style={styles.title}>SplitCard</Text>

      <TextInput
        style={styles.input}
        placeholder="Email"
        autoCapitalize="none"
        keyboardType="email-address"
        value={email}
        onChangeText={setEmail}
      />

      <TextInput
        style={styles.input}
        placeholder="Contraseña"
        secureTextEntry
        value={password}
        onChangeText={setPassword}
      />

      {formError ? <Text style={styles.error}>{formError}</Text> : null}

      <Pressable style={styles.button} onPress={handleSubmit} disabled={isAuthenticating}>
        <Text style={styles.buttonText}>{isAuthenticating ? 'Ingresando...' : 'Ingresar'}</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', padding: 24, gap: 12 },
  title: { fontSize: 28, fontWeight: '700', marginBottom: 24, textAlign: 'center' },
  input: { borderWidth: 1, borderColor: '#ccc', borderRadius: 8, padding: 12, fontSize: 16 },
  button: { backgroundColor: '#111', borderRadius: 8, padding: 14, alignItems: 'center', marginTop: 8 },
  buttonText: { color: '#fff', fontSize: 16, fontWeight: '600' },
  error: { color: '#c0392b' },
});
