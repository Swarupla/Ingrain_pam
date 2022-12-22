import { TestBed } from '@angular/core/testing';

import { HyperTuningService } from './hyper-tuning.service';

describe('HyperTuningService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: HyperTuningService = TestBed.get(HyperTuningService);
    expect(service).toBeTruthy();
  });
});
