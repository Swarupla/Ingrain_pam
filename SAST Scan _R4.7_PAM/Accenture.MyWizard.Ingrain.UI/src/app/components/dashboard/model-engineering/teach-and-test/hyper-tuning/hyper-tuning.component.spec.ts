import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { HyperTuningComponent } from './hyper-tuning.component';

describe('HyperTuningComponent', () => {
  let component: HyperTuningComponent;
  let fixture: ComponentFixture<HyperTuningComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ HyperTuningComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(HyperTuningComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
