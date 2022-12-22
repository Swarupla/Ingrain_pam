import { TestBed } from '@angular/core/testing';

import { AdProblemStatementService } from './ad-problem-statement.service';

describe('AdProblemStatementService', () => {
  let service: AdProblemStatementService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AdProblemStatementService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
