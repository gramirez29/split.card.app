import { useState } from 'react';
import { ScrollView, StyleSheet, View } from 'react-native';
import { Button, HelperText, Surface, Text, TextInput } from 'react-native-paper';
import { router } from 'expo-router';
import { registerHousehold } from '@/api/endpoints/households';
import { useAuth } from '@/hooks/useAuth';

export default function RegisterScreen() {
  const { login } = useAuth();

  const [householdName, setHouseholdName] = useState('');
  const [ownerName, setOwnerName] = useState('');
  const [ownerEmail, setOwnerEmail] = useState('');
  const [ownerPassword, setOwnerPassword] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  async function handleSubmit() {
    setFormError(null);

    if (!householdName || !ownerName || !ownerEmail || !ownerPassword) {
      setFormError('Completá todos los campos.');
      return;
    }

    setIsSubmitting(true);

    const result = await registerHousehold({ householdName, ownerName, ownerEmail, ownerPassword });

    if (!result.ok) {
      setIsSubmitting(false);
      setFormError(result.error.message);
      return;
    }

    // El household y el Owner ya existen en el backend en este punto — logueamos
    // automáticamente con las mismas credenciales para no obligar a tipearlas de nuevo.
    const loggedIn = await login(ownerEmail, ownerPassword);
    setIsSubmitting(false);

    if (!loggedIn) {
      setFormError('Hogar creado, pero falló el login automático. Iniciá sesión manualmente.');
      router.replace('/(auth)/login');
      return;
    }

    router.replace('/(app)');
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent} keyboardShouldPersistTaps="handled">
      <View style={styles.header}>
        <Text variant="headlineMedium" style={styles.title}>
          Creá tu hogar
        </Text>
        <Text variant="bodyMedium" style={styles.subtitle}>
          Esto crea tu household y tu cuenta como Owner en un solo paso.
        </Text>
      </View>

      <Surface style={styles.card} elevation={1}>
        <TextInput
          mode="outlined"
          label="Nombre del hogar"
          placeholder="Familia Rodríguez"
          value={householdName}
          onChangeText={setHouseholdName}
          style={styles.input}
        />

        <TextInput mode="outlined" label="Tu nombre" value={ownerName} onChangeText={setOwnerName} style={styles.input} />

        <TextInput
          mode="outlined"
          label="Email"
          autoCapitalize="none"
          keyboardType="email-address"
          value={ownerEmail}
          onChangeText={setOwnerEmail}
          style={styles.input}
        />

        <TextInput
          mode="outlined"
          label="Contraseña"
          secureTextEntry
          value={ownerPassword}
          onChangeText={setOwnerPassword}
          style={styles.input}
        />

        <HelperText type="error" visible={formError !== null}>
          {formError ?? ''}
        </HelperText>

        <Button
          mode="contained"
          onPress={handleSubmit}
          loading={isSubmitting}
          disabled={isSubmitting}
          style={styles.submitButton}
          contentStyle={styles.submitButtonContent}
        >
          {isSubmitting ? 'Creando...' : 'Crear hogar'}
        </Button>
      </Surface>

      <Button mode="text" onPress={() => router.back()} style={styles.backLink}>
        Ya tengo cuenta
      </Button>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  scrollContent: { flexGrow: 1, justifyContent: 'center', padding: 24 },
  header: { marginBottom: 32, alignItems: 'center' },
  title: { fontWeight: '700' },
  subtitle: { marginTop: 8, textAlign: 'center', opacity: 0.7 },
  card: { borderRadius: 16, padding: 24 },
  input: { marginBottom: 12 },
  submitButton: { marginTop: 8, borderRadius: 8 },
  submitButtonContent: { paddingVertical: 6 },
  backLink: { marginTop: 16 },
});
