import { TestBed } from '@angular/core/testing';

import { TeachTestService } from './teach-test.service';

describe('TeachTestService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: TeachTestService = TestBed.get(TeachTestService);
    expect(service).toBeTruthy();
  });
});
