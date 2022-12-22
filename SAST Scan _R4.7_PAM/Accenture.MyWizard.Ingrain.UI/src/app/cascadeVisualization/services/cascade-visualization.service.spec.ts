import { TestBed } from '@angular/core/testing';

import { CascadeVisualizationService } from './cascade-visualization.service';

describe('CascadeVisualizationService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: CascadeVisualizationService = TestBed.get(CascadeVisualizationService);
    expect(service).toBeTruthy();
  });
});
