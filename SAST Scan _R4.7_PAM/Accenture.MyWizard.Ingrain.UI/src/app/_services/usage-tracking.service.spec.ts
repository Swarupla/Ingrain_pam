import { TestBed } from '@angular/core/testing';

import { UsageTrackingService } from './usage-tracking.service';

describe('UsageTrackingService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: UsageTrackingService = TestBed.get(UsageTrackingService);
    expect(service).toBeTruthy();
  });
});
