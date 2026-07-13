import { useState } from 'react';
import { StyleSheet, ScrollView } from 'react-native';
import { Button, HelperText, SegmentedButtons, Surface, Text, TextInput } from 'react-native-paper';
import { router } from 'expo-router';
import { useAuth } from '@/hooks/useAuth';
import { useHouseholdStore } from '@/store/householdStore';
import { theme, spacing, radius } from '@/constants/theme';
import type { UserRole } from '@/types/enums';

// Role solo puede ser Contributor o RestrictedViewer — el backend rechaza invitar un
// segundo Owner por este endpoint (ApplicationValidationException, 400).
type InvitableRole = Extract<UserRole, 'Contributor' | 'RestrictedViewer'>;

export default function InviteMemberScreen() {
  const { user } = useAuth();
  const { isSubmitting, inviteMember } = useHouseholdStore();

  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [role, setRole] = useState<InvitableRole>('Contributor');
  const [formError, setFormError] = useState<string | null>(null);

  if (!user) {
    return null;
  }

  async function handleSubmit() {
    if (!user) {
      return;
    }

    setFormError(null);

    if (!name || !email || !password) {
      setFormError('Completá nombre, email y contraseña.');
      return;
    }

    const success = await inviteMember({
      householdId: user.householdId,
      name,
      email,
      password,
      role,
    });

    if (!success) {
      setFormError(useHouseholdStore.getState().error ?? 'No se pudo invitar al miembro.');
      return;
    }

    router.back();
  }

  return (
    <ScrollView style={styles.flex} contentContainerStyle={styles.scrollContent}>
      <Surface style={styles.card} elevation={0}>
        <TextInput mode="outlined" label="Nombre" value={name} onChangeText={setName} style={styles.input} />

        <TextInput
          mode="outlined"
          label="Email"
          autoCapitalize="none"
          keyboardType="email-address"
          value={email}
          onChangeText={setEmail}
          style={styles.input}
        />

        <TextInput
          mode="outlined"
          label="Contraseña"
          secureTextEntry
          value={password}
          onChangeText={setPassword}
          style={styles.input}
        />

        <Text variant="labelLarge" style={styles.label}>
          Rol
        </Text>
        <SegmentedButtons
          value={role}
          onValueChange={(value) => setRole(value as InvitableRole)}
          buttons={[
            { value: 'Contributor', label: 'Contributor' },
            { value: 'RestrictedViewer', label: 'Restricted' },
          ]}
          style={styles.segmented}
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
          {isSubmitting ? 'Invitando...' : 'Invitar'}
        </Button>
      </Surface>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  flex: { flex: 1, backgroundColor: theme.colors.background },
  scrollContent: { padding: spacing.md, paddingBottom: spacing.xl },
  card: { borderRadius: radius.lg, padding: spacing.md, backgroundColor: theme.colors.surface },
  input: { marginBottom: spacing.md },
  label: { color: theme.colors.onSurfaceVariant, marginBottom: spacing.sm },
  segmented: { marginBottom: spacing.md },
  submitButton: { borderRadius: radius.pill, marginTop: spacing.sm },
  submitButtonContent: { paddingVertical: 6 },
});
