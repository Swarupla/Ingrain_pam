import { TestBed } from '@angular/core/testing';

import { FeatureSelectionService } from './feature-selection.service';

describe('FeatureSelectionService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: FeatureSelectionService = TestBed.get(FeatureSelectionService);
    expect(service).toBeTruthy();
  });
});
