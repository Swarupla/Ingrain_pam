import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ProblemStatementComponent } from './problem-statement.component';

describe('ProblemStatementComponent', () => {
  let component: ProblemStatementComponent;
  let fixture: ComponentFixture<ProblemStatementComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ProblemStatementComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ProblemStatementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
