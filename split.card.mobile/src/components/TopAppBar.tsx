import { View, StyleSheet, Pressable } from 'react-native';
import { Icon, Text } from 'react-native-paper';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { theme, spacing, radius } from '@/constants/theme';

// Altura del contenido de la barra, sin contar el inset de la status bar — los 4 tabs
// la usan para calcular su padding superior (insets.top + APP_BAR_HEIGHT + spacing).
export const APP_BAR_HEIGHT = 56;

interface TopAppBarProps {
  onMenuPress: () => void;
}

/**
 * Barra flotente fija arriba de las 4 pantallas de tabs — mismo tratamiento visual que la
 * barra inferior (misma elevation/shadow/radius) para que ambas se sientan como el mismo
 * sistema, "flotando" sobre el contenido en vez de empujarlo. No usa el header nativo de
 * React Navigation a propósito: un header nativo reserva espacio y empuja el contenido
 * hacia abajo — acá el contenido tiene que poder scrollear por debajo de la barra.
 */
export function TopAppBar({ onMenuPress }: TopAppBarProps) {
  const insets = useSafeAreaInsets();

  return (
    <View style={[styles.container, { paddingTop: insets.top }]}>
      <View style={styles.bar}>
        <Pressable onPress={onMenuPress} style={styles.iconButton} hitSlop={8}>
          <Icon source="menu" size={24} color={theme.colors.onSurface} />
        </Pressable>

        <Text variant="titleMedium" style={styles.title}>
          SplitCard
        </Text>

        <View style={styles.iconButton} />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    zIndex: 10,
    backgroundColor: theme.colors.surface,
    borderBottomLeftRadius: radius.lg + 4,
    borderBottomRightRadius: radius.lg + 4,
    elevation: 8,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 12,
  },
  bar: {
    height: APP_BAR_HEIGHT,
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: spacing.sm,
  },
  iconButton: {
    width: 40,
    height: 40,
    alignItems: 'center',
    justifyContent: 'center',
  },
  title: {
    flex: 1,
    textAlign: 'center',
    color: theme.colors.onSurface,
    fontWeight: '700',
  },
});
