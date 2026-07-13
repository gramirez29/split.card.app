import { useState } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, View } from 'react-native';
import { Button, HelperText, Surface, Text, TextInput } from 'react-native-paper';
import { router } from 'expo-router';
import { useAuth } from '@/hooks/useAuth';

export default function LoginScreen() {
  const { login, isAuthenticating } = useAuth();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [passwordVisible, setPasswordVisible] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  const emailError = formError !== null && email.length === 0;
  const passwordError = formError !== null && password.length === 0;

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
      setFormError('Credenciales inválidas. Verifica tu email y contraseña.');
    }
  }

  return (
    <KeyboardAvoidingView
      style={styles.flex}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        keyboardShouldPersistTaps="handled"
      >
        <View style={styles.header}>
          <Text variant="headlineMedium" style={styles.title}>
            SplitCard
          </Text>
          <Text variant="bodyMedium" style={styles.subtitle}>
            Registra tus compras y controla tus cuotas compartidas
          </Text>
        </View>

        <Surface style={styles.card} elevation={1}>
          <Text variant="titleLarge" style={styles.cardTitle}>
            Iniciar sesión
          </Text>

          <TextInput
            mode="outlined"
            label="Email"
            placeholder="tu@email.com"
            autoCapitalize="none"
            autoComplete="email"
            keyboardType="email-address"
            left={<TextInput.Icon icon="email-outline" />}
            value={email}
            onChangeText={setEmail}
            error={emailError}
            style={styles.input}
          />

          <TextInput
            mode="outlined"
            label="Contraseña"
            autoCapitalize="none"
            autoComplete="password"
            secureTextEntry={!passwordVisible}
            left={<TextInput.Icon icon="lock-outline" />}
            right={
              <TextInput.Icon
                icon={passwordVisible ? 'eye-off-outline' : 'eye-outline'}
                onPress={() => setPasswordVisible((visible) => !visible)}
                forceTextInputFocus={false}
              />
            }
            value={password}
            onChangeText={setPassword}
            error={passwordError}
            style={styles.input}
          />

          <HelperText type="error" visible={formError !== null}>
            {formError ?? ''}
          </HelperText>

          <Button
            mode="contained"
            onPress={handleSubmit}
            loading={isAuthenticating}
            disabled={isAuthenticating}
            style={styles.submitButton}
            contentStyle={styles.submitButtonContent}
          >
            {isAuthenticating ? 'Ingresando...' : 'Ingresar'}
          </Button>
        </Surface>

        <Button mode="text" onPress={() => router.push('/(auth)/register')} style={styles.registerLink}>
          ¿No tenés cuenta? Creá tu hogar
        </Button>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  flex: {
    flex: 1,
  },
  scrollContent: {
    flexGrow: 1,
    justifyContent: 'center',
    padding: 24,
  },
  header: {
    marginBottom: 32,
    alignItems: 'center',
  },
  title: {
    fontWeight: '700',
  },
  subtitle: {
    marginTop: 8,
    textAlign: 'center',
    opacity: 0.7,
  },
  card: {
    borderRadius: 16,
    padding: 24,
  },
  cardTitle: {
    marginBottom: 20,
  },
  input: {
    marginBottom: 12,
  },
  submitButton: {
    marginTop: 8,
    borderRadius: 8,
  },
  submitButtonContent: {
    paddingVertical: 6,
  },
  registerLink: {
    marginTop: 16,
  },
});
