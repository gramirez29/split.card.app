import { useState } from 'react';
import { View, StyleSheet, Pressable, FlatList } from 'react-native';
import type { StyleProp, ViewStyle } from 'react-native';
import { Icon, Modal, Portal, Text } from 'react-native-paper';
import { theme, spacing, radius } from '@/constants/theme';

export interface DropdownOption {
  label: string;
  value: string;
}

interface DropdownProps {
  label: string;
  value: string;
  options: DropdownOption[];
  onSelect: (value: string) => void;
  style?: StyleProp<ViewStyle>;
}

/**
 * Custom bottom-sheet "select", not react-native-paper's Menu. Menu anchors itself via
 * an absolute-position measurement of its anchor element, which is unreliable inside a
 * ScrollView (this is exactly what caused options to render outside the visible content
 * area) and only makes the anchor's own intrinsic content tappable, not the full field.
 * A Portal+Modal sheet sidesteps both problems: it's a full-screen overlay (no anchor
 * math at all) and the entire field row is a single Pressable we control end to end.
 */
export function Dropdown({ label, value, options, onSelect, style }: DropdownProps) {
  const [isVisible, setIsVisible] = useState(false);

  const selectedLabel = options.find((option) => option.value === value)?.label ?? null;

  function handleSelect(optionValue: string) {
    onSelect(optionValue);
    setIsVisible(false);
  }

  return (
    <>
      <Pressable
        onPress={() => setIsVisible(true)}
        android_ripple={{ color: theme.colors.outlineVariant }}
        style={({ pressed }) => [styles.field, style, pressed && styles.fieldPressed]}
      >
        <View style={styles.fieldTextGroup}>
          <Text variant="labelSmall" style={styles.fieldLabel}>
            {label}
          </Text>
          <Text
            variant="bodyLarge"
            style={selectedLabel ? styles.fieldValue : styles.fieldPlaceholder}
            numberOfLines={1}
          >
            {selectedLabel ?? 'Seleccioná una opción'}
          </Text>
        </View>
        <Icon source="chevron-down" size={22} color={theme.colors.onSurfaceVariant} />
      </Pressable>

      <Portal>
        <Modal
          visible={isVisible}
          onDismiss={() => setIsVisible(false)}
          style={styles.modalOuter}
          contentContainerStyle={styles.modalContent}
        >
          <View style={styles.sheetHandle} />

          <Text variant="titleMedium" style={styles.modalTitle}>
            {label}
          </Text>

          <FlatList
            data={options}
            keyExtractor={(item) => item.value}
            style={styles.optionsList}
            showsVerticalScrollIndicator={false}
            renderItem={({ item }) => {
              const isSelected = item.value === value;

              return (
                <Pressable
                  onPress={() => handleSelect(item.value)}
                  android_ripple={{ color: theme.colors.outlineVariant }}
                  style={({ pressed }) => [
                    styles.option,
                    isSelected && styles.optionSelected,
                    pressed && styles.optionPressed,
                  ]}
                >
                  <Text
                    variant="bodyLarge"
                    style={isSelected ? styles.optionTextSelected : styles.optionText}
                  >
                    {item.label}
                  </Text>
                  {isSelected ? <Icon source="check" size={20} color={theme.colors.primary} /> : null}
                </Pressable>
              );
            }}
          />
        </Modal>
      </Portal>
    </>
  );
}

const styles = StyleSheet.create({
  field: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderWidth: 1,
    borderColor: theme.colors.outline,
    borderRadius: radius.sm,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    backgroundColor: theme.colors.surface,
  },
  fieldPressed: {
    backgroundColor: theme.colors.elevation.level3,
  },
  fieldTextGroup: { flex: 1, marginRight: spacing.sm },
  fieldLabel: { color: theme.colors.onSurfaceVariant },
  fieldValue: { color: theme.colors.onSurface, marginTop: 2 },
  fieldPlaceholder: { color: theme.colors.onSurfaceVariant, marginTop: 2 },
  modalOuter: {
    justifyContent: 'flex-end',
    margin: 0,
  },
  modalContent: {
    backgroundColor: theme.colors.surface,
    borderTopLeftRadius: radius.lg,
    borderTopRightRadius: radius.lg,
    paddingTop: spacing.sm,
    paddingHorizontal: spacing.md,
    paddingBottom: spacing.lg,
    maxHeight: '70%',
  },
  sheetHandle: {
    alignSelf: 'center',
    width: 40,
    height: 4,
    borderRadius: radius.pill,
    backgroundColor: theme.colors.outline,
    marginBottom: spacing.sm,
  },
  modalTitle: {
    color: theme.colors.onSurface,
    marginBottom: spacing.sm,
    paddingHorizontal: spacing.xs,
  },
  optionsList: { flexGrow: 0 },
  option: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.sm,
    borderRadius: radius.sm,
  },
  optionSelected: {
    backgroundColor: theme.colors.primaryContainer,
  },
  optionPressed: {
    backgroundColor: theme.colors.elevation.level3,
  },
  optionText: { color: theme.colors.onSurface },
  optionTextSelected: { color: theme.colors.onPrimaryContainer, fontWeight: '600' },
});
