import { TestBed } from '@angular/core/testing';

import { MyVisualizationService } from './my-visualization.service';

describe('MyVisualizationService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: MyVisualizationService = TestBed.get(MyVisualizationService);
    expect(service).toBeTruthy();
  });
});
