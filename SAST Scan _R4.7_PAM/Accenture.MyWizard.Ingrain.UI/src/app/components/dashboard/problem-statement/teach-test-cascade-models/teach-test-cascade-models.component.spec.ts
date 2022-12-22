import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TeachTestCascadeModelsComponent } from './teach-test-cascade-models.component';

describe('TeachTestCascadeModelsComponent', () => {
  let component: TeachTestCascadeModelsComponent;
  let fixture: ComponentFixture<TeachTestCascadeModelsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TeachTestCascadeModelsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TeachTestCascadeModelsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
