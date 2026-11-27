import type { TFunction } from 'i18next';
import type { ArrangementShapeId } from './layout';
import { arrangementShapeLabel } from '../lib/enumLabels';

export function shapeOptions(t: TFunction) {
  return ([0, 1, 2] as ArrangementShapeId[]).map((id) => ({
    id,
    label: arrangementShapeLabel(id, t),
  }));
}
